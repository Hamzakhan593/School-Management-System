using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.ExamController + "," + AppRoles.Teacher)]
public class ResultsController : Controller
{
    private const string ManageRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.ExamController;

    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IResultService _results;
    private readonly IResultPdfService _pdf;
    private readonly IAuditService _audit;

    public ResultsController(ApplicationDbContext db, ISchoolContextService schoolContext, IResultService results, IResultPdfService pdf, IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _results = results;
        _pdf = pdf;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? sessionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
        var selectedSessionId = sessionId ?? sessions.FirstOrDefault(x => x.Status == AcademicSessionStatus.Active)?.Id ?? sessions.FirstOrDefault()?.Id;

        var query = _db.Exams.AsNoTracking()
            .Include(x => x.AcademicSession)
            .Include(x => x.Term)
            .Include(x => x.ExamClasses).ThenInclude(x => x.SchoolClass)
            .Where(x => x.SchoolId == context.Value.SchoolId && (x.Status == ExamStatus.MarksLocked || x.Status == ExamStatus.Published || x.Status == ExamStatus.Archived));

        if (selectedSessionId.HasValue) query = query.Where(x => x.AcademicSessionId == selectedSessionId.Value);

        if (!IsManager())
        {
            query = query.Where(exam => _db.TeacherAssignments.Any(a =>
                a.SchoolId == context.Value.SchoolId
                && a.AcademicSessionId == exam.AcademicSessionId
                && a.TeacherUserId == context.Value.UserId
                && a.IsActive
                && exam.ExamClasses.Any(ec => ec.SchoolClassId == a.SchoolClassId)));
        }

        var exams = await query.OrderByDescending(x => x.StartDate).ThenBy(x => x.Title).ToListAsync(cancellationToken);
        return View(new ResultsIndexViewModel { SelectedSessionId = selectedSessionId, Sessions = sessions, Exams = exams });
    }

    [HttpGet]
    public async Task<IActionResult> ClassResult(int examId, int classId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!await CanAccessClassAsync(context.Value, examId, classId, sectionId, cancellationToken)) return Forbid();

        var model = await BuildClassResultAsync(context.Value.SchoolId, examId, classId, sectionId, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int examId, int classId, int? sectionId, string? correctionReason, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        try
        {
            var published = await _results.PublishClassResultsAsync(context.Value.SchoolId, context.Value.UserId, examId, classId, sectionId, correctionReason, cancellationToken);
            await _audit.WriteAsync("Result.Published", "Exam", examId.ToString(), $"ClassId={classId}; SectionId={sectionId}; Count={published.Count}; Correction={correctionReason}");
            TempData["Success"] = correctionReason is null ? $"Published {published.Count} result(s)." : $"Published corrected version for {published.Count} result(s).";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(ClassResult), new { examId, classId, sectionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BeginCorrection(int examId, string reason, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A correction reason is required.";
            return RedirectToAction(nameof(Index));
        }

        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.Published)
        {
            TempData["Error"] = "Only a published exam can enter correction mode.";
            return RedirectToAction(nameof(Index));
        }

        exam.Status = ExamStatus.MarksEntryOpen;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Result.CorrectionStarted", "Exam", exam.Id.ToString(), reason.Trim());
        TempData["Info"] = "Correction mode started. Reopen only the required marks sheet in Exams & Marks, edit it, verify/lock it again, then Lock Exam and publish a corrected result version here.";
        return RedirectToAction("Details", "Exams", new { id = examId });
    }

    [HttpGet]
    public async Task<IActionResult> ResultCard(int examId, int studentId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await BuildResultCardAsync(context.Value, examId, studentId, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResultCardPdf(int examId, int studentId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await BuildResultCardAsync(context.Value, examId, studentId, cancellationToken);
        if (model is null) return NotFound();
        var bytes = await _pdf.CreateResultCardPdfAsync(model, cancellationToken);
        return File(bytes, "application/pdf", $"Result-{Safe(model.Student.AdmissionNumber)}-{Safe(model.Exam.Title)}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> BulkResultCardsPdf(int examId, int classId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!await CanAccessClassAsync(context.Value, examId, classId, sectionId, cancellationToken)) return Forbid();

        var studentIds = await _db.StudentResults.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId && x.ExamId == examId && x.IsCurrent
                && x.StudentEnrollment.SchoolClassId == classId
                && (!sectionId.HasValue || x.StudentEnrollment.SectionId == sectionId.Value))
            .OrderBy(x => x.StudentEnrollment.RollNumber)
            .ThenBy(x => x.Student.FullName)
            .Select(x => x.StudentId)
            .ToListAsync(cancellationToken);

        var cards = new List<ResultCardViewModel>();
        foreach (var studentId in studentIds)
        {
            var card = await BuildResultCardAsync(context.Value, examId, studentId, cancellationToken);
            if (card is not null) cards.Add(card);
        }

        if (cards.Count == 0)
        {
            TempData["Error"] = "Publish results before generating result cards.";
            return RedirectToAction(nameof(ClassResult), new { examId, classId, sectionId });
        }

        var bytes = await _pdf.CreateBulkResultCardsPdfAsync(cards, cancellationToken);
        return File(bytes, "application/pdf", $"ResultCards-Exam-{examId}-Class-{classId}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectedResultCardsPdf(int examId, int classId, int? sectionId, List<int> studentIds, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!await CanAccessClassAsync(context.Value, examId, classId, sectionId, cancellationToken)) return Forbid();
        if (studentIds.Count == 0)
        {
            TempData["Error"] = "Select at least one published student result card.";
            return RedirectToAction(nameof(ClassResult), new { examId, classId, sectionId });
        }

        var allowedIds = await _db.StudentResults.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId && x.ExamId == examId && x.IsCurrent
                && x.StudentEnrollment.SchoolClassId == classId
                && (!sectionId.HasValue || x.StudentEnrollment.SectionId == sectionId.Value)
                && studentIds.Contains(x.StudentId))
            .Select(x => x.StudentId)
            .ToListAsync(cancellationToken);

        var cards = new List<ResultCardViewModel>();
        foreach (var studentId in allowedIds.Distinct())
        {
            var card = await BuildResultCardAsync(context.Value, examId, studentId, cancellationToken);
            if (card is not null) cards.Add(card);
        }
        if (cards.Count == 0)
        {
            TempData["Error"] = "No published result cards were found for the selected students.";
            return RedirectToAction(nameof(ClassResult), new { examId, classId, sectionId });
        }
        return File(await _pdf.CreateBulkResultCardsPdfAsync(cards, cancellationToken), "application/pdf", $"Selected-ResultCards-Exam-{examId}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> AllResultCardsPdf(int examId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();

        var resultRows = await _db.StudentResults.AsNoTracking()
            .Include(x => x.StudentEnrollment)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.ExamId == examId && x.IsCurrent)
            .OrderBy(x => x.StudentEnrollment.ClassName)
            .ThenBy(x => x.StudentEnrollment.SectionName)
            .ThenBy(x => x.StudentEnrollment.RollNumber)
            .Select(x => new { x.StudentId, x.StudentEnrollment.SchoolClassId, x.StudentEnrollment.SectionId })
            .ToListAsync(cancellationToken);

        var cards = new List<ResultCardViewModel>();
        foreach (var row in resultRows)
        {
            if (!row.SchoolClassId.HasValue) continue;
            if (!await CanAccessClassAsync(context.Value, examId, row.SchoolClassId.Value, row.SectionId, cancellationToken)) continue;
            var card = await BuildResultCardAsync(context.Value, examId, row.StudentId, cancellationToken);
            if (card is not null) cards.Add(card);
        }
        if (cards.Count == 0)
        {
            TempData["Error"] = "No published result cards are available for this exam.";
            return RedirectToAction(nameof(Index));
        }
        return File(await _pdf.CreateBulkResultCardsPdfAsync(cards, cancellationToken), "application/pdf", $"All-ResultCards-Exam-{examId}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ClassResultSheetPdf(int examId, int classId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!await CanAccessClassAsync(context.Value, examId, classId, sectionId, cancellationToken)) return Forbid();
        var model = await BuildClassResultAsync(context.Value.SchoolId, examId, classId, sectionId, cancellationToken);
        if (model is null) return NotFound();
        var bytes = await _pdf.CreateClassResultSheetPdfAsync(model, cancellationToken);
        return File(bytes, "application/pdf", $"ClassResult-Exam-{examId}-Class-{classId}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> SubjectPerformance(int examId, int classId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!await CanAccessClassAsync(context.Value, examId, classId, sectionId, cancellationToken)) return Forbid();

        var exam = await _db.Exams.AsNoTracking().Include(x => x.AcademicSession).FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        var schoolClass = await _db.SchoolClasses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == classId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null || schoolClass is null) return NotFound();
        var section = sectionId.HasValue ? await _db.Sections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sectionId && x.SchoolClassId == classId, cancellationToken) : null;
        var sections = await _db.Sections.AsNoTracking().Where(x => x.SchoolClassId == classId && x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);

        var marks = await _db.StudentMarks.AsNoTracking()
            .Include(x => x.ExamSubject).ThenInclude(x => x.Subject)
            .Include(x => x.StudentEnrollment)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.ExamId == examId
                && x.ExamSubject.SchoolClassId == classId
                && (!sectionId.HasValue || x.StudentEnrollment.SectionId == sectionId.Value))
            .ToListAsync(cancellationToken);

        var rows = marks.GroupBy(x => new { x.ExamSubjectId, x.ExamSubject.Subject.Title, x.ExamSubject.MaxMarks, x.ExamSubject.PassMarks })
            .Select(g =>
            {
                var countable = g.Where(x => x.SpecialStatus != MarkSpecialStatus.Exempt).ToList();
                var numeric = countable.Where(x => x.SpecialStatus == MarkSpecialStatus.None).ToList();
                var passed = numeric.Count(x => (x.ObtainedMarks ?? 0) >= g.Key.PassMarks);
                var failed = numeric.Count - passed;
                var absent = countable.Count(x => x.SpecialStatus == MarkSpecialStatus.Absent);
                var avg = numeric.Count == 0 ? 0 : numeric.Average(x => x.ObtainedMarks ?? 0);
                var denominator = countable.Count;
                return new SubjectPerformanceRowViewModel
                {
                    Subject = g.Key.Title,
                    StudentCount = denominator,
                    PassedCount = passed,
                    FailedCount = failed,
                    AbsentCount = absent,
                    AverageMarks = Math.Round(avg, 2),
                    MaximumMarks = g.Key.MaxMarks,
                    AveragePercentage = g.Key.MaxMarks <= 0 ? 0 : Math.Round(avg / g.Key.MaxMarks * 100m, 2),
                    PassRate = denominator == 0 ? 0 : Math.Round((decimal)passed / denominator * 100m, 2)
                };
            })
            .OrderBy(x => x.Subject)
            .ToList();

        return View(new SubjectPerformanceViewModel { Exam = exam, SchoolClass = schoolClass, Section = section, Sections = sections, Subjects = rows });
    }

    private async Task<ClassResultViewModel?> BuildClassResultAsync(int schoolId, int examId, int classId, int? sectionId, CancellationToken cancellationToken)
    {
        var exam = await _db.Exams.AsNoTracking()
            .Include(x => x.School)
            .Include(x => x.AcademicSession)
            .Include(x => x.ExamSubjects)
            .FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == schoolId, cancellationToken);
        var schoolClass = await _db.SchoolClasses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == classId && x.SchoolId == schoolId, cancellationToken);
        if (exam is null || schoolClass is null) return null;
        var section = sectionId.HasValue ? await _db.Sections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sectionId && x.SchoolClassId == classId, cancellationToken) : null;
        var sections = await _db.Sections.AsNoTracking().Where(x => x.SchoolClassId == classId && x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);

        var enrollments = await _db.StudentEnrollments.AsNoTracking().Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == exam.AcademicSessionId && x.SchoolClassId == classId
                && x.Status == StudentEnrollmentStatus.Active && x.Student.Status == StudentStatus.Active
                && (!sectionId.HasValue || x.SectionId == sectionId.Value))
            .OrderBy(x => x.RollNumber).ThenBy(x => x.Student.FullName)
            .ToListAsync(cancellationToken);

        var studentIds = enrollments.Select(x => x.StudentId).ToList();
        var snapshots = await _db.StudentResults.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.ExamId == examId && x.IsCurrent && studentIds.Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        var rows = new List<ClassResultRowViewModel>();
        foreach (var enrollment in enrollments)
        {
            if (snapshots.TryGetValue(enrollment.StudentId, out var snapshot))
            {
                rows.Add(new ClassResultRowViewModel
                {
                    StudentId = enrollment.StudentId,
                    StudentEnrollmentId = enrollment.Id,
                    AdmissionNumber = enrollment.Student.AdmissionNumber,
                    RollNumber = enrollment.RollNumber ?? enrollment.Student.RollNumber,
                    StudentName = enrollment.Student.FullName,
                    ObtainedMarks = snapshot.ObtainedMarks,
                    MaximumMarks = snapshot.MaximumMarks,
                    Percentage = snapshot.Percentage,
                    Grade = snapshot.Grade,
                    IsPassed = snapshot.IsPassed,
                    Position = snapshot.ClassPosition,
                    AttendancePercentage = snapshot.AttendancePercentage,
                    PublishedVersion = snapshot.VersionNumber
                });
                continue;
            }

            var calc = await _results.CalculateStudentExamAsync(schoolId, examId, enrollment.StudentId, cancellationToken);
            if (calc is null) continue;
            rows.Add(new ClassResultRowViewModel
            {
                StudentId = enrollment.StudentId,
                StudentEnrollmentId = enrollment.Id,
                AdmissionNumber = enrollment.Student.AdmissionNumber,
                RollNumber = enrollment.RollNumber ?? enrollment.Student.RollNumber,
                StudentName = enrollment.Student.FullName,
                ObtainedMarks = calc.ObtainedMarks,
                MaximumMarks = calc.MaximumMarks,
                Percentage = calc.Percentage,
                Grade = await _results.ResolveGradeAsync(exam.AcademicSessionId, calc.Percentage, calc.Passed, cancellationToken),
                IsPassed = calc.Passed,
                AttendancePercentage = await _results.CalculateAttendancePercentageAsync(schoolId, exam.AcademicSessionId, enrollment.StudentId, exam.EndDate, cancellationToken)
            });
        }

        if (snapshots.Count == 0)
        {
            var ranked = rows.OrderByDescending(x => x.Percentage).ThenByDescending(x => x.ObtainedMarks).ToList();
            int position = 0;
            decimal? lastPercentage = null;
            decimal? lastObtained = null;
            for (var i = 0; i < ranked.Count; i++)
            {
                if (lastPercentage != ranked[i].Percentage || lastObtained != ranked[i].ObtainedMarks) position = i + 1;
                ranked[i].Position = position;
                lastPercentage = ranked[i].Percentage;
                lastObtained = ranked[i].ObtainedMarks;
            }
        }

        return new ClassResultViewModel
        {
            Exam = exam,
            SchoolClass = schoolClass,
            Section = section,
            Sections = sections,
            Rows = rows.OrderBy(x => x.RollNumber).ThenBy(x => x.StudentName).ToList(),
            HasPublishedResults = snapshots.Count > 0,
            CanPublish = exam.Status == ExamStatus.MarksLocked || exam.Status == ExamStatus.Published,
            IsCorrectionInProgress = exam.Status == ExamStatus.MarksEntryOpen && snapshots.Count > 0
        };
    }

    private async Task<ResultCardViewModel?> BuildResultCardAsync((int SchoolId, string UserId) context, int examId, int studentId, CancellationToken cancellationToken)
    {
        var result = await _db.StudentResults.AsNoTracking()
            .Include(x => x.School)
            .Include(x => x.Exam).ThenInclude(x => x.AcademicSession)
            .Include(x => x.Student)
            .Include(x => x.StudentEnrollment)
            .FirstOrDefaultAsync(x => x.SchoolId == context.SchoolId && x.ExamId == examId && x.StudentId == studentId && x.IsCurrent, cancellationToken);
        if (result is null) return null;
        if (!await CanAccessClassAsync(context, examId, result.StudentEnrollment.SchoolClassId ?? 0, result.StudentEnrollment.SectionId, cancellationToken)) return null;

        var marks = await _db.StudentMarks.AsNoTracking()
            .Include(x => x.ExamSubject).ThenInclude(x => x.Subject)
            .Where(x => x.SchoolId == context.SchoolId && x.ExamId == examId && x.StudentId == studentId)
            .OrderBy(x => x.ExamSubject.Subject.Title)
            .ToListAsync(cancellationToken);

        var subjectRows = marks.Select(x => new ResultCardSubjectRowViewModel
        {
            Subject = x.ExamSubject.Subject.Title,
            MaximumMarks = x.ExamSubject.MaxMarks,
            PassMarks = x.ExamSubject.PassMarks,
            ObtainedMarks = x.ObtainedMarks,
            SpecialStatus = x.SpecialStatus,
            Passed = x.SpecialStatus == MarkSpecialStatus.Exempt || (x.SpecialStatus == MarkSpecialStatus.None && (x.ObtainedMarks ?? 0) >= x.ExamSubject.PassMarks),
            Remarks = x.TeacherRemarks
        }).ToList();

        return new ResultCardViewModel
        {
            School = result.School,
            Exam = result.Exam,
            Student = result.Student,
            Enrollment = result.StudentEnrollment,
            Result = result,
            Subjects = subjectRows
        };
    }

    private async Task<bool> CanAccessClassAsync((int SchoolId, string UserId) context, int examId, int classId, int? sectionId, CancellationToken cancellationToken)
    {
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == context.SchoolId, cancellationToken);
        if (exam is null) return false;
        if (IsManager()) return true;
        return await _db.TeacherAssignments.AsNoTracking().AnyAsync(x => x.SchoolId == context.SchoolId
            && x.AcademicSessionId == exam.AcademicSessionId
            && x.TeacherUserId == context.UserId
            && x.SchoolClassId == classId
            && x.IsActive
            && (!x.SectionId.HasValue || !sectionId.HasValue || x.SectionId == sectionId), cancellationToken);
    }

    private bool IsManager() => User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ExamController);

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is null || string.IsNullOrWhiteSpace(user.Id)) return null;
        return (user.SchoolId.Value, user.Id);
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school before using results.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private static string Safe(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
    }
}
