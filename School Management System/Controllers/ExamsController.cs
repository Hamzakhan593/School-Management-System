using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.ExamController + "," + AppRoles.Teacher)]
public class ExamsController : Controller
{
    private const string ManageRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.ExamController;

    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IExamService _examService;
    private readonly IAuditService _audit;

    public ExamsController(ApplicationDbContext db, ISchoolContextService schoolContext, IExamService examService, IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _examService = examService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? sessionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId && x.Status != AcademicSessionStatus.Archived)
            .OrderByDescending(x => x.Status == AcademicSessionStatus.Active)
            .ThenByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
        var selectedSessionId = sessionId ?? sessions.FirstOrDefault()?.Id;

        var query = _db.Exams.AsNoTracking()
            .Include(x => x.AcademicSession)
            .Include(x => x.Term)
            .Include(x => x.ExamClasses).ThenInclude(x => x.SchoolClass)
            .Where(x => x.SchoolId == context.Value.SchoolId);

        if (selectedSessionId.HasValue)
            query = query.Where(x => x.AcademicSessionId == selectedSessionId.Value);

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
        return View(new ExamIndexViewModel { SelectedSessionId = selectedSessionId, Sessions = sessions, Exams = exams });
    }

    [HttpGet]
    public async Task<IActionResult> Workspace(int? examId, int? classId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var subjects = _db.ExamSubjects.AsNoTracking().Include(x => x.Exam).ThenInclude(x => x.AcademicSession)
            .Include(x => x.SchoolClass).Include(x => x.Subject)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.IsActive && x.Exam.IsActive && x.Exam.Status != ExamStatus.Draft);
        if (!IsManager()) subjects = subjects.Where(x => _db.TeacherAssignments.Any(a => a.SchoolId == context.Value.SchoolId
            && a.AcademicSessionId == x.Exam.AcademicSessionId && a.SchoolClassId == x.SchoolClassId
            && a.SubjectId == x.SubjectId && a.TeacherUserId == context.Value.UserId && a.IsActive));
        var available = await subjects.OrderByDescending(x => x.Exam.StartDate).ThenBy(x => x.SchoolClass.SortOrder).ThenBy(x => x.Subject.Title).ToListAsync(cancellationToken);
        var exams = available.Select(x => x.Exam).DistinctBy(x => x.Id).ToList();
        examId ??= exams.FirstOrDefault(x => x.Status == ExamStatus.MarksEntryOpen)?.Id ?? exams.FirstOrDefault()?.Id;
        var selected = available.Where(x => x.ExamId == examId).ToList();
        return View(new MarksWorkspaceViewModel { ExamId = examId, ClassId = classId, Exams = exams, Subjects = selected });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> Create(int? sessionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = new ExamFormViewModel { AcademicSessionId = sessionId ?? 0, StartDate = DateTime.Today, EndDate = DateTime.Today };
        await PopulateExamFormAsync(model, context.Value.SchoolId, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamFormViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        await ValidateExamFormAsync(model, context.Value.SchoolId, cancellationToken);
        if (!ModelState.IsValid)
        {
            await PopulateExamFormAsync(model, context.Value.SchoolId, cancellationToken);
            return View(model);
        }

        var exam = new Exam
        {
            SchoolId = context.Value.SchoolId,
            AcademicSessionId = model.AcademicSessionId,
            TermId = model.TermId,
            Title = model.Title.Trim(),
            ExamType = model.ExamType.Trim(),
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            IsActive = model.IsActive,
            Status = ExamStatus.Draft,
            CreatedByUserId = context.Value.UserId
        };
        _db.Exams.Add(exam);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.Created", "Exam", exam.Id.ToString(), $"{exam.Title}; SessionId={exam.AcademicSessionId}");
        TempData["Success"] = "Exam created. Now assign classes and subjects.";
        return RedirectToAction(nameof(Details), new { id = exam.Id });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.Draft)
        {
            TempData["Error"] = "Exam setup can only be edited while the exam is Draft.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new ExamFormViewModel
        {
            Id = exam.Id,
            AcademicSessionId = exam.AcademicSessionId,
            TermId = exam.TermId,
            Title = exam.Title,
            ExamType = exam.ExamType,
            StartDate = exam.StartDate,
            EndDate = exam.EndDate,
            IsActive = exam.IsActive
        };
        await PopulateExamFormAsync(model, context.Value.SchoolId, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExamFormViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == model.Id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.Draft)
        {
            TempData["Error"] = "Exam setup can only be edited while the exam is Draft.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        await ValidateExamFormAsync(model, context.Value.SchoolId, cancellationToken);
        if (!ModelState.IsValid)
        {
            await PopulateExamFormAsync(model, context.Value.SchoolId, cancellationToken);
            return View(model);
        }

        exam.AcademicSessionId = model.AcademicSessionId;
        exam.TermId = model.TermId;
        exam.Title = model.Title.Trim();
        exam.ExamType = model.ExamType.Trim();
        exam.StartDate = model.StartDate.Date;
        exam.EndDate = model.EndDate.Date;
        exam.IsActive = model.IsActive;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.Updated", "Exam", exam.Id.ToString(), exam.Title);
        TempData["Success"] = "Exam updated.";
        return RedirectToAction(nameof(Details), new { id = exam.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var exam = await _db.Exams.AsNoTracking()
            .Include(x => x.AcademicSession)
            .Include(x => x.Term)
            .Include(x => x.ExamClasses).ThenInclude(x => x.SchoolClass)
            .Include(x => x.ExamSubjects).ThenInclude(x => x.Subject)
            .Include(x => x.ExamSubjects).ThenInclude(x => x.SchoolClass)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();

        var assignedClassIds = exam.ExamClasses.Select(x => x.SchoolClassId).ToList();
        if (!IsManager())
        {
            var canSee = await _db.TeacherAssignments.AsNoTracking().AnyAsync(a =>
                a.SchoolId == context.Value.SchoolId
                && a.AcademicSessionId == exam.AcademicSessionId
                && a.TeacherUserId == context.Value.UserId
                && a.IsActive
                && assignedClassIds.Contains(a.SchoolClassId), cancellationToken);
            if (!canSee) return Forbid();
        }

        var availableClasses = await _db.SchoolClasses.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var availableClassSubjects = await _db.ClassSubjects.AsNoTracking()
            .Include(x => x.SchoolClass)
            .Include(x => x.Subject)
            .Where(x => x.SchoolId == context.Value.SchoolId
                && x.AcademicSessionId == exam.AcademicSessionId
                && x.IsActive
                && assignedClassIds.Contains(x.SchoolClassId)
                && x.Subject.IsActive)
            .OrderBy(x => x.SchoolClass.SortOrder).ThenBy(x => x.Subject.Title)
            .ToListAsync(cancellationToken);

        var marksSheets = await _db.ExamMarksSheets.AsNoTracking()
            .Include(x => x.Section)
            .Include(x => x.ExamSubject).ThenInclude(x => x.Subject)
            .Include(x => x.ExamSubject).ThenInclude(x => x.SchoolClass)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.ExamId == exam.Id)
            .ToListAsync(cancellationToken);

        return View(new ExamDetailsViewModel
        {
            Exam = exam,
            AvailableClasses = availableClasses,
            AvailableClassSubjects = availableClassSubjects,
            MarksSheets = marksSheets
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClass(int examId, int schoolClassId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.Draft) return SetupLocked(examId);
        if (!await _db.SchoolClasses.AnyAsync(x => x.Id == schoolClassId && x.SchoolId == context.Value.SchoolId && x.IsActive, cancellationToken))
        {
            TempData["Error"] = "Select a valid class.";
            return RedirectToAction(nameof(Details), new { id = examId });
        }
        if (await _db.ExamClasses.AnyAsync(x => x.ExamId == examId && x.SchoolClassId == schoolClassId, cancellationToken))
        {
            TempData["Info"] = "This class is already assigned to the exam.";
            return RedirectToAction(nameof(Details), new { id = examId });
        }

        var entity = new ExamClass { SchoolId = context.Value.SchoolId, ExamId = examId, SchoolClassId = schoolClassId };
        _db.ExamClasses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.ClassAssigned", "ExamClass", entity.Id.ToString(), $"ExamId={examId}; ClassId={schoolClassId}");
        TempData["Success"] = "Class assigned to exam.";
        return RedirectToAction(nameof(Details), new { id = examId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveClass(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var item = await _db.ExamClasses.Include(x => x.Exam).FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (item is null) return NotFound();
        if (item.Exam.Status != ExamStatus.Draft) return SetupLocked(item.ExamId);
        if (await _db.ExamSubjects.AnyAsync(x => x.ExamId == item.ExamId && x.SchoolClassId == item.SchoolClassId, cancellationToken))
        {
            TempData["Error"] = "Remove this class's exam subjects first.";
            return RedirectToAction(nameof(Details), new { id = item.ExamId });
        }
        _db.ExamClasses.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.ClassRemoved", "ExamClass", id.ToString(), $"ExamId={item.ExamId}; ClassId={item.SchoolClassId}");
        TempData["Success"] = "Class removed from exam.";
        return RedirectToAction(nameof(Details), new { id = item.ExamId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSubject(ExamSubjectFormViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == model.ExamId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.Draft) return SetupLocked(exam.Id);
        await ValidateExamSubjectAsync(model, exam, context.Value.SchoolId, cancellationToken);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Details), new { id = exam.Id });
        }

        var entity = new ExamSubject
        {
            SchoolId = context.Value.SchoolId,
            ExamId = exam.Id,
            SchoolClassId = model.SchoolClassId,
            SubjectId = model.SubjectId,
            MaxMarks = model.MaxMarks,
            PassMarks = model.PassMarks,
            TheoryMaxMarks = PositiveOrNull(model.TheoryMaxMarks),
            PracticalMaxMarks = PositiveOrNull(model.PracticalMaxMarks),
            WeightagePercent = model.WeightagePercent,
            IsActive = true
        };
        _db.ExamSubjects.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.SubjectConfigured", "ExamSubject", entity.Id.ToString(), $"ExamId={exam.Id}; ClassId={model.SchoolClassId}; SubjectId={model.SubjectId}");
        TempData["Success"] = "Exam subject configured.";
        return RedirectToAction(nameof(Details), new { id = exam.Id });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSubject(int id, decimal maxMarks, decimal passMarks, decimal? theoryMaxMarks, decimal? practicalMaxMarks, decimal weightagePercent, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var subject = await _db.ExamSubjects.Include(x => x.Exam).FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (subject is null) return NotFound();
        if (subject.Exam.Status != ExamStatus.Draft) return SetupLocked(subject.ExamId);
        var model = new ExamSubjectFormViewModel { ExamId = subject.ExamId, SchoolClassId = subject.SchoolClassId, SubjectId = subject.SubjectId, MaxMarks = maxMarks, PassMarks = passMarks, TheoryMaxMarks = theoryMaxMarks, PracticalMaxMarks = practicalMaxMarks, WeightagePercent = weightagePercent };
        await ValidateExamSubjectAsync(model, subject.Exam, context.Value.SchoolId, cancellationToken, subject.Id);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Details), new { id = subject.ExamId });
        }
        subject.MaxMarks = maxMarks;
        subject.PassMarks = passMarks;
        subject.TheoryMaxMarks = PositiveOrNull(theoryMaxMarks);
        subject.PracticalMaxMarks = PositiveOrNull(practicalMaxMarks);
        subject.WeightagePercent = weightagePercent;
        subject.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.SubjectUpdated", "ExamSubject", subject.Id.ToString(), $"Max={maxMarks}; Pass={passMarks}; Weightage={weightagePercent}");
        TempData["Success"] = "Exam subject settings updated.";
        return RedirectToAction(nameof(Details), new { id = subject.ExamId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSubject(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var subject = await _db.ExamSubjects.Include(x => x.Exam).FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (subject is null) return NotFound();
        if (subject.Exam.Status != ExamStatus.Draft) return SetupLocked(subject.ExamId);
        if (await _db.StudentMarks.AnyAsync(x => x.ExamSubjectId == subject.Id, cancellationToken))
        {
            TempData["Error"] = "This subject already has marks and cannot be removed.";
            return RedirectToAction(nameof(Details), new { id = subject.ExamId });
        }
        var examId = subject.ExamId;
        _db.ExamSubjects.Remove(subject);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.SubjectRemoved", "ExamSubject", id.ToString(), $"ExamId={examId}");
        TempData["Success"] = "Exam subject removed.";
        return RedirectToAction(nameof(Details), new { id = examId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenMarks(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.Draft)
        {
            TempData["Error"] = "Only a Draft exam can be opened for marks entry.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (!await _db.ExamClasses.AnyAsync(x => x.ExamId == id, cancellationToken) || !await _db.ExamSubjects.AnyAsync(x => x.ExamId == id && x.IsActive, cancellationToken))
        {
            TempData["Error"] = "Assign at least one class and configure at least one exam subject first.";
            return RedirectToAction(nameof(Details), new { id });
        }
        exam.Status = ExamStatus.MarksEntryOpen;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.MarksEntryOpened", "Exam", exam.Id.ToString(), exam.Title);
        TempData["Success"] = "Marks entry is now open. Exam setup is locked.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Marks(int examSubjectId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await _examService.BuildMarksEntryAsync(context.Value.SchoolId, context.Value.UserId, IsManager(), examSubjectId, sectionId, cancellationToken);
        if (model is null) return Forbid();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMarks(MarksEntryViewModel model, CancellationToken cancellationToken, bool submit = false)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = ModelState.IsValid
            ? await _examService.SaveMarksAsync(context.Value.SchoolId, context.Value.UserId, IsManager(), model, cancellationToken)
            : new ExamOperationResult(false, "Please enter valid numbers in the highlighted fields.");
        if (!result.Success)
        {
            var redisplay = await _examService.BuildMarksEntryAsync(context.Value.SchoolId, context.Value.UserId, IsManager(), model.ExamSubjectId, model.SectionId, cancellationToken);
            if (redisplay is null) return Forbid();
            var posted = model.Rows.GroupBy(x => x.StudentId).ToDictionary(x => x.Key, x => x.First());
            // Restore inputs by student identity; never bind old row indexes to a changed roster.
            foreach (var row in redisplay.Rows)
                if (posted.TryGetValue(row.StudentId, out var input))
                { row.TheoryMarks = input.TheoryMarks; row.PracticalMarks = input.PracticalMarks; row.SpecialStatus = input.SpecialStatus; row.TeacherRemarks = input.TeacherRemarks; }
            ModelState.Clear();
            ModelState.AddModelError("", result.Message);
            return View("Marks", redisplay);
        }
        if (submit) result = await _examService.SubmitMarksAsync(context.Value.SchoolId, context.Value.UserId, IsManager(), model.ExamMarksSheetId, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (result.Success)
            await _audit.WriteAsync("Exam.MarksDraftSaved", "ExamMarksSheet", model.ExamMarksSheetId.ToString(), $"Rows={model.Rows.Count}");
        return RedirectToAction(nameof(Marks), new { examSubjectId = model.ExamSubjectId, sectionId = model.SectionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitMarks(int sheetId, int examSubjectId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _examService.SubmitMarksAsync(context.Value.SchoolId, context.Value.UserId, IsManager(), sheetId, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (result.Success) await _audit.WriteAsync("Exam.MarksSubmitted", "ExamMarksSheet", sheetId.ToString());
        return RedirectToAction(nameof(Marks), new { examSubjectId, sectionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportClassSubjects(int examId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.Include(x => x.ExamClasses).Include(x => x.ExamSubjects)
            .FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.Draft) return SetupLocked(examId);
        var classIds = exam.ExamClasses.Select(x => x.SchoolClassId).ToList();
        var mappings = await _db.ClassSubjects.Include(x => x.Subject).Where(x => x.SchoolId == context.Value.SchoolId
            && x.AcademicSessionId == exam.AcademicSessionId && classIds.Contains(x.SchoolClassId) && x.IsActive && x.Subject.IsActive).ToListAsync(cancellationToken);
        var added = 0;
        foreach (var mapping in mappings)
        {
            if (exam.ExamSubjects.Any(x => x.SchoolClassId == mapping.SchoolClassId && x.SubjectId == mapping.SubjectId)) continue;
            var max = mapping.MaxMarks ?? mapping.Subject.DefaultMaxMarks ?? 100m;
            var pass = mapping.PassMarks ?? mapping.Subject.DefaultPassMarks ?? max * .4m;
            if (max <= 0 || pass < 0 || pass > max) { TempData["Error"] = $"Correct maximum/pass marks for {mapping.Subject.Title} in Classes before copying subjects."; return RedirectToAction(nameof(Details), new { id = examId }); }
            exam.ExamSubjects.Add(new ExamSubject { SchoolId = context.Value.SchoolId, ExamId = examId, SchoolClassId = mapping.SchoolClassId,
                SubjectId = mapping.SubjectId, MaxMarks = max, PassMarks = pass, WeightagePercent = 100m, IsActive = true });
            added++;
        }
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.SubjectsImported", "Exam", examId.ToString(), $"Subjects={added}");
        TempData["Success"] = $"Added {added} subjects from Classes. Review maximum and pass marks before opening marks entry.";
        return RedirectToAction(nameof(Details), new { id = examId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveMarks(int sheetId, int examSubjectId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _examService.VerifyMarksAsync(context.Value.SchoolId, context.Value.UserId, sheetId, cancellationToken);
        if (result.Success) result = await _examService.LockMarksAsync(context.Value.SchoolId, context.Value.UserId, sheetId, cancellationToken);
        if (result.Success) await _audit.WriteAsync("Exam.MarksApprovedAndLocked", "ExamMarksSheet", sheetId.ToString());
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Marks approved and locked for result generation." : result.Message;
        return RedirectToAction(nameof(Marks), new { examSubjectId, sectionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyMarks(int sheetId, int examSubjectId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _examService.VerifyMarksAsync(context.Value.SchoolId, context.Value.UserId, sheetId, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (result.Success) await _audit.WriteAsync("Exam.MarksVerified", "ExamMarksSheet", sheetId.ToString());
        return RedirectToAction(nameof(Marks), new { examSubjectId, sectionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockMarks(int sheetId, int examSubjectId, int? sectionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _examService.LockMarksAsync(context.Value.SchoolId, context.Value.UserId, sheetId, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (result.Success) await _audit.WriteAsync("Exam.MarksSheetLocked", "ExamMarksSheet", sheetId.ToString());
        return RedirectToAction(nameof(Marks), new { examSubjectId, sectionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenMarks(int sheetId, int examSubjectId, int? sectionId, string reason, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _examService.ReopenMarksAsync(context.Value.SchoolId, context.Value.UserId, sheetId, reason, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (result.Success) await _audit.WriteAsync("Exam.MarksSheetReopened", "ExamMarksSheet", sheetId.ToString(), reason);
        return RedirectToAction(nameof(Marks), new { examSubjectId, sectionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockExam(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var exam = await _db.Exams.Include(x => x.ExamSubjects).FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (exam is null) return NotFound();
        if (exam.Status != ExamStatus.MarksEntryOpen)
        {
            TempData["Error"] = "The exam must be open for marks entry before final locking.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var expectedSheetKeys = new List<(int ExamSubjectId, int? SectionId)>();
        foreach (var subject in exam.ExamSubjects.Where(x => x.IsActive))
        {
            var sectionIds = await _db.StudentEnrollments.AsNoTracking()
                .Where(x => x.SchoolId == context.Value.SchoolId
                    && x.AcademicSessionId == exam.AcademicSessionId
                    && x.SchoolClassId == subject.SchoolClassId
                    && x.Status == StudentEnrollmentStatus.Active
                    && x.Student.Status == StudentStatus.Active)
                .Select(x => x.SectionId)
                .Distinct()
                .ToListAsync(cancellationToken);
            expectedSheetKeys.AddRange(sectionIds.Select(sectionId => (subject.Id, sectionId)));
        }

        if (expectedSheetKeys.Count == 0)
        {
            TempData["Error"] = "No active student marks sheets are expected for this exam.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var sheets = await _db.ExamMarksSheets.AsNoTracking().Where(x => x.ExamId == id).ToListAsync(cancellationToken);
        var incomplete = expectedSheetKeys.Any(key => !sheets.Any(s => s.ExamSubjectId == key.ExamSubjectId && s.SectionId == key.SectionId && s.Status == ExamMarksSheetStatus.Locked));
        if (incomplete)
        {
            TempData["Error"] = "Every class/section subject marks sheet must be verified and locked first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        exam.Status = ExamStatus.MarksLocked;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Exam.AllMarksLocked", "Exam", exam.Id.ToString(), exam.Title);
        TempData["Success"] = "All exam marks are locked. M10 can now calculate and publish results.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool IsManager() => User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ExamController);

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is null || string.IsNullOrWhiteSpace(user.Id)) return null;
        return (user.SchoolId.Value, user.Id);
    }

    private async Task PopulateExamFormAsync(ExamFormViewModel model, int schoolId, CancellationToken cancellationToken)
    {
        model.Sessions = await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == schoolId && x.Status != AcademicSessionStatus.Archived).OrderByDescending(x => x.StartDate).ToListAsync(cancellationToken);
        if (model.AcademicSessionId == 0)
            model.AcademicSessionId = model.Sessions.FirstOrDefault(x => x.Status == AcademicSessionStatus.Active)?.Id ?? model.Sessions.FirstOrDefault()?.Id ?? 0;
        model.Terms = model.AcademicSessionId > 0
            ? await _db.Terms.AsNoTracking().Where(x => x.AcademicSessionId == model.AcademicSessionId).OrderBy(x => x.DisplayOrder).ThenBy(x => x.StartDate).ToListAsync(cancellationToken)
            : [];
    }

    private async Task ValidateExamFormAsync(ExamFormViewModel model, int schoolId, CancellationToken cancellationToken)
    {
        if (model.EndDate.Date < model.StartDate.Date) ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before start date.");
        if (!await _db.AcademicSessions.AnyAsync(x => x.Id == model.AcademicSessionId && x.SchoolId == schoolId, cancellationToken))
            ModelState.AddModelError(nameof(model.AcademicSessionId), "Select a valid academic session.");
        if (model.TermId.HasValue && !await _db.Terms.AnyAsync(x => x.Id == model.TermId && x.AcademicSessionId == model.AcademicSessionId, cancellationToken))
            ModelState.AddModelError(nameof(model.TermId), "Selected term does not belong to this academic session.");
    }

    private async Task ValidateExamSubjectAsync(ExamSubjectFormViewModel model, Exam exam, int schoolId, CancellationToken cancellationToken, int? ignoreId = null)
    {
        if (model.PassMarks > model.MaxMarks) ModelState.AddModelError(nameof(model.PassMarks), "Pass marks cannot exceed maximum marks.");
        var theory = PositiveOrNull(model.TheoryMaxMarks) ?? 0m;
        var practical = PositiveOrNull(model.PracticalMaxMarks) ?? 0m;
        if (theory + practical > 0 && theory + practical != model.MaxMarks)
            ModelState.AddModelError(nameof(model.MaxMarks), "Theory + practical maximum marks must equal total maximum marks.");
        if (!await _db.ExamClasses.AnyAsync(x => x.ExamId == exam.Id && x.SchoolClassId == model.SchoolClassId, cancellationToken))
            ModelState.AddModelError(nameof(model.SchoolClassId), "Assign this class to the exam first.");
        if (!await _db.ClassSubjects.AnyAsync(x => x.SchoolId == schoolId && x.AcademicSessionId == exam.AcademicSessionId && x.SchoolClassId == model.SchoolClassId && x.SubjectId == model.SubjectId && x.IsActive, cancellationToken))
            ModelState.AddModelError(nameof(model.SubjectId), "This subject is not mapped to the selected class for the exam session.");
        if (await _db.ExamSubjects.AnyAsync(x => x.ExamId == exam.Id && x.SchoolClassId == model.SchoolClassId && x.SubjectId == model.SubjectId && (!ignoreId.HasValue || x.Id != ignoreId.Value), cancellationToken))
            ModelState.AddModelError(nameof(model.SubjectId), "This exam subject is already configured for the class.");
    }

    private IActionResult SetupLocked(int examId)
    {
        TempData["Error"] = "Exam setup is locked after marks entry is opened.";
        return RedirectToAction(nameof(Details), new { id = examId });
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school before using examinations.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private string FirstError() => ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault() ?? "Please correct the form.";
    private static decimal? PositiveOrNull(decimal? value) => value.HasValue && value.Value > 0 ? value : null;
}
