using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class ExamService : IExamService
{
    private readonly ApplicationDbContext _db;

    public ExamService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MarksEntryViewModel?> BuildMarksEntryAsync(
        int schoolId,
        string userId,
        bool isExamManager,
        int examSubjectId,
        int? sectionId,
        CancellationToken cancellationToken = default)
    {
        var examSubject = await _db.ExamSubjects
            .AsNoTracking()
            .Include(x => x.Exam).ThenInclude(x => x.AcademicSession)
            .Include(x => x.SchoolClass)
            .Include(x => x.Subject)
            .FirstOrDefaultAsync(x => x.Id == examSubjectId && x.SchoolId == schoolId && x.IsActive, cancellationToken);

        if (examSubject is null || !examSubject.Exam.IsActive)
            return null;

        var sections = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.SchoolClassId == examSubject.SchoolClassId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var allowUnsectioned = isExamManager;
        if (!isExamManager)
        {
            var assignments = await _db.TeacherAssignments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId
                    && x.AcademicSessionId == examSubject.Exam.AcademicSessionId
                    && x.SchoolClassId == examSubject.SchoolClassId
                    && x.SubjectId == examSubject.SubjectId
                    && x.TeacherUserId == userId
                    && x.IsActive)
                .ToListAsync(cancellationToken);

            if (assignments.Count == 0)
                return null;

            allowUnsectioned = assignments.Any(x => x.SectionId == null);

            if (!assignments.Any(x => x.SectionId == null))
            {
                var allowedIds = assignments.Where(x => x.SectionId.HasValue).Select(x => x.SectionId!.Value).ToHashSet();
                sections = sections.Where(x => allowedIds.Contains(x.Id)).ToList();
                if (sections.Count == 0)
                    return null;
            }
        }

        if (allowUnsectioned && await _db.StudentEnrollments.AnyAsync(x => x.SchoolId == schoolId
            && x.AcademicSessionId == examSubject.Exam.AcademicSessionId && x.SchoolClassId == examSubject.SchoolClassId
            && x.SectionId == null && x.Status == StudentEnrollmentStatus.Active && x.Student.Status == StudentStatus.Active, cancellationToken))
            sections.Add(new Section { Id = 0, Name = "No section assigned" });

        var selectedSectionId = sectionId;
        if (sections.Count > 0)
        {
            if (!selectedSectionId.HasValue)
                selectedSectionId = sections[0].Id;
            if (!sections.Any(x => x.Id == selectedSectionId.Value))
                return null;
        }
        else
        {
            if (selectedSectionId.HasValue && selectedSectionId != 0) return null;
            selectedSectionId = null;
        }
        if (selectedSectionId == 0) selectedSectionId = null;

        var sheet = await _db.ExamMarksSheets
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId
                && x.ExamSubjectId == examSubject.Id
                && x.SectionId == selectedSectionId, cancellationToken);

        if (sheet is null)
        {
            if (examSubject.Exam.Status != ExamStatus.MarksEntryOpen)
                return null;

            sheet = new ExamMarksSheet
            {
                SchoolId = schoolId,
                ExamId = examSubject.ExamId,
                ExamSubjectId = examSubject.Id,
                SectionId = selectedSectionId,
                Status = ExamMarksSheetStatus.Draft
            };
            _db.ExamMarksSheets.Add(sheet);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var enrollmentQuery = _db.StudentEnrollments.AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == examSubject.Exam.AcademicSessionId
                && x.SchoolClassId == examSubject.SchoolClassId
                && x.Status == StudentEnrollmentStatus.Active
                && x.Student.Status == StudentStatus.Active);

        enrollmentQuery = enrollmentQuery.Where(x => x.SectionId == selectedSectionId);

        var enrollments = await enrollmentQuery
            .OrderBy(x => x.RollNumber)
            .ThenBy(x => x.Student.FullName)
            .ToListAsync(cancellationToken);

        var existingMarks = await _db.StudentMarks.AsNoTracking()
            .Where(x => x.ExamMarksSheetId == sheet.Id)
            .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        var canEdit = examSubject.Exam.Status == ExamStatus.MarksEntryOpen
            && sheet.Status == ExamMarksSheetStatus.Draft;

        return new MarksEntryViewModel
        {
            ExamId = examSubject.ExamId,
            ExamSubjectId = examSubject.Id,
            ExamMarksSheetId = sheet.Id,
            SchoolClassId = examSubject.SchoolClassId,
            SectionId = selectedSectionId ?? (sections.Any(x => x.Id == 0) ? 0 : null),
            ExamTitle = examSubject.Exam.Title,
            SessionName = examSubject.Exam.AcademicSession.Name,
            ClassName = examSubject.SchoolClass.Name,
            SubjectName = examSubject.Subject.Title,
            SectionName = sections.FirstOrDefault(x => x.Id == (selectedSectionId ?? 0))?.Name,
            MaxMarks = examSubject.MaxMarks,
            PassMarks = examSubject.PassMarks,
            TheoryMaxMarks = examSubject.TheoryMaxMarks,
            PracticalMaxMarks = examSubject.PracticalMaxMarks,
            ExamStatus = examSubject.Exam.Status,
            SheetStatus = sheet.Status,
            CanEdit = canEdit,
            CanSubmit = examSubject.Exam.Status == ExamStatus.MarksEntryOpen && sheet.Status == ExamMarksSheetStatus.Draft,
            CanVerify = isExamManager && sheet.Status == ExamMarksSheetStatus.Submitted,
            CanLock = isExamManager && sheet.Status == ExamMarksSheetStatus.Verified,
            CanReopen = isExamManager && sheet.Status != ExamMarksSheetStatus.Draft && examSubject.Exam.Status != ExamStatus.Published,
            AvailableSections = sections,
            Rows = enrollments.Select(x =>
            {
                existingMarks.TryGetValue(x.StudentId, out var mark);
                return new MarkEntryRowViewModel
                {
                    StudentId = x.StudentId,
                    StudentEnrollmentId = x.Id,
                    AdmissionNumber = x.Student.AdmissionNumber,
                    RollNumber = x.RollNumber,
                    StudentName = x.Student.FullName,
                    SpecialStatus = mark?.SpecialStatus ?? MarkSpecialStatus.None,
                    TheoryMarks = mark?.TheoryMarks,
                    PracticalMarks = mark?.PracticalMarks,
                    TeacherRemarks = mark?.TeacherRemarks
                };
            }).ToList()
        };
    }

    public async Task<ExamOperationResult> SaveMarksAsync(
        int schoolId,
        string userId,
        bool isExamManager,
        MarksEntryViewModel model,
        CancellationToken cancellationToken = default)
    {
        var allowed = await BuildMarksEntryAsync(schoolId, userId, isExamManager, model.ExamSubjectId, model.SectionId, cancellationToken);
        if (allowed is null || allowed.ExamMarksSheetId != model.ExamMarksSheetId)
            return new(false, "Marks sheet was not found or you do not have access.");
        if (!allowed.CanEdit)
            return new(false, "This marks sheet is not editable in its current workflow state.");

        if (model.Rows.GroupBy(x => x.StudentId).Any(g => g.Count() > 1))
            return new(false, "Duplicate student rows were submitted. Reload the marks sheet and try again.");

        var posted = model.Rows.ToDictionary(x => x.StudentId);
        var allowedIds = allowed.Rows.Select(x => x.StudentId).ToHashSet();
        if (posted.Count != allowedIds.Count || posted.Keys.Any(x => !allowedIds.Contains(x)))
            return new(false, "The class roster changed. Reload the marks sheet before saving.");

        var examSubject = await _db.ExamSubjects.FirstAsync(x => x.Id == model.ExamSubjectId && x.SchoolId == schoolId, cancellationToken);
        var existing = await _db.StudentMarks
            .Where(x => x.ExamMarksSheetId == model.ExamMarksSheetId)
            .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        foreach (var rosterRow in allowed.Rows)
        {
            var row = posted[rosterRow.StudentId];
            if (!Enum.IsDefined(row.SpecialStatus))
                return new(false, $"{rosterRow.StudentName}: Select a valid attendance status.");
            if (row.TeacherRemarks?.Length > 500)
                return new(false, $"{rosterRow.StudentName}: Keep remarks within 500 characters.");
            // An untouched row is an incomplete draft, never a zero or an absence.
            if (row.SpecialStatus == MarkSpecialStatus.None && !row.TheoryMarks.HasValue && !row.PracticalMarks.HasValue)
            {
                if (existing.TryGetValue(rosterRow.StudentId, out var cleared)) _db.StudentMarks.Remove(cleared);
                continue;
            }
            decimal? theory = null;
            decimal? practical = null;
            decimal? obtained = null;

            if (row.SpecialStatus == MarkSpecialStatus.None)
            {
                var validation = ValidateAndCalculate(examSubject, row);
                if (!validation.Success)
                    return new(false, $"{rosterRow.StudentName}: {validation.Message}");
                theory = validation.Theory;
                practical = validation.Practical;
                obtained = validation.Total;
            }

            if (!existing.TryGetValue(rosterRow.StudentId, out var mark))
            {
                mark = new StudentMark
                {
                    SchoolId = schoolId,
                    ExamId = examSubject.ExamId,
                    ExamSubjectId = examSubject.Id,
                    ExamMarksSheetId = model.ExamMarksSheetId,
                    StudentId = rosterRow.StudentId,
                    StudentEnrollmentId = rosterRow.StudentEnrollmentId
                };
                _db.StudentMarks.Add(mark);
            }

            mark.StudentEnrollmentId = rosterRow.StudentEnrollmentId;
            mark.SpecialStatus = row.SpecialStatus;
            mark.TheoryMarks = row.SpecialStatus == MarkSpecialStatus.None ? theory : null;
            mark.PracticalMarks = row.SpecialStatus == MarkSpecialStatus.None ? practical : null;
            mark.ObtainedMarks = row.SpecialStatus == MarkSpecialStatus.None ? obtained : null;
            mark.TeacherRemarks = string.IsNullOrWhiteSpace(row.TeacherRemarks) ? null : row.TeacherRemarks.Trim();
            mark.EnteredByUserId = userId;
            mark.EnteredAtUtc ??= DateTime.UtcNow;
            mark.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new(true, $"Draft marks saved for {allowed.Rows.Count} student(s).", model.ExamMarksSheetId);
    }

    public async Task<ExamOperationResult> SubmitMarksAsync(int schoolId, string userId, bool isExamManager, int sheetId, CancellationToken cancellationToken = default)
    {
        var access = await LoadSheetWithAccessAsync(schoolId, userId, isExamManager, sheetId, cancellationToken);
        if (access is null) return new(false, "Marks sheet not found or access denied.");
        var sheet = access.Value.Sheet;
        if (sheet.Exam.Status != ExamStatus.MarksEntryOpen) return new(false, "Marks entry is not open for this exam.");
        if (sheet.Status != ExamMarksSheetStatus.Draft) return new(false, "Only draft marks can be submitted.");

        var expected = await ExpectedStudentCountAsync(sheet, cancellationToken);
        var entered = await _db.StudentMarks.CountAsync(x => x.ExamMarksSheetId == sheet.Id
            && (x.SpecialStatus != MarkSpecialStatus.None || x.ObtainedMarks.HasValue)
            && x.StudentEnrollment.Status == StudentEnrollmentStatus.Active && x.Student.Status == StudentStatus.Active
            && x.StudentEnrollment.AcademicSessionId == sheet.Exam.AcademicSessionId
            && x.StudentEnrollment.SchoolClassId == sheet.ExamSubject.SchoolClassId
            && x.StudentEnrollment.SectionId == sheet.SectionId, cancellationToken);
        if (expected == 0) return new(false, "There are no active students in this class/section.");
        if (entered != expected) return new(false, $"Marks are incomplete. Expected {expected} student(s), but {entered} row(s) are saved.");

        sheet.Status = ExamMarksSheetStatus.Submitted;
        sheet.SubmittedByUserId = userId;
        sheet.SubmittedAtUtc = DateTime.UtcNow;
        sheet.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Marks submitted for verification.", sheet.Id);
    }

    public async Task<ExamOperationResult> VerifyMarksAsync(int schoolId, string userId, int sheetId, CancellationToken cancellationToken = default)
    {
        var sheet = await LoadManagerSheetAsync(schoolId, sheetId, cancellationToken);
        if (sheet is null) return new(false, "Marks sheet not found.");
        if (sheet.Status != ExamMarksSheetStatus.Submitted) return new(false, "Only submitted marks can be verified.");
        sheet.Status = ExamMarksSheetStatus.Verified;
        sheet.VerifiedByUserId = userId;
        sheet.VerifiedAtUtc = DateTime.UtcNow;
        sheet.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Marks verified. They can now be locked.", sheet.Id);
    }

    public async Task<ExamOperationResult> LockMarksAsync(int schoolId, string userId, int sheetId, CancellationToken cancellationToken = default)
    {
        var sheet = await LoadManagerSheetAsync(schoolId, sheetId, cancellationToken);
        if (sheet is null) return new(false, "Marks sheet not found.");
        if (sheet.Status != ExamMarksSheetStatus.Verified) return new(false, "Only verified marks can be locked.");
        sheet.Status = ExamMarksSheetStatus.Locked;
        sheet.LockedByUserId = userId;
        sheet.LockedAtUtc = DateTime.UtcNow;
        sheet.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Marks sheet locked.", sheet.Id);
    }

    public async Task<ExamOperationResult> ReopenMarksAsync(int schoolId, string userId, int sheetId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "A reason is required to reopen marks.");
        var sheet = await LoadManagerSheetAsync(schoolId, sheetId, cancellationToken);
        if (sheet is null) return new(false, "Marks sheet not found.");
        if (sheet.Exam.Status == ExamStatus.Published) return new(false, "Published results cannot be reopened from M09.");
        if (sheet.Status == ExamMarksSheetStatus.Draft) return new(false, "This sheet is already a draft.");

        sheet.Status = ExamMarksSheetStatus.Draft;
        sheet.ReopenedByUserId = userId;
        sheet.ReopenedAtUtc = DateTime.UtcNow;
        sheet.ReopenReason = reason.Trim();
        sheet.VerifiedByUserId = null;
        sheet.VerifiedAtUtc = null;
        sheet.LockedByUserId = null;
        sheet.LockedAtUtc = null;
        sheet.UpdatedAtUtc = DateTime.UtcNow;
        if (sheet.Exam.Status == ExamStatus.MarksLocked)
            sheet.Exam.Status = ExamStatus.MarksEntryOpen;

        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Marks sheet reopened as draft.", sheet.Id);
    }

    private async Task<(ExamMarksSheet Sheet, bool IsManager)?> LoadSheetWithAccessAsync(
        int schoolId,
        string userId,
        bool isExamManager,
        int sheetId,
        CancellationToken cancellationToken)
    {
        var sheet = await _db.ExamMarksSheets
            .Include(x => x.Exam)
            .Include(x => x.ExamSubject)
            .FirstOrDefaultAsync(x => x.Id == sheetId && x.SchoolId == schoolId, cancellationToken);
        if (sheet is null) return null;
        if (isExamManager) return (sheet, true);

        var allowed = await _db.TeacherAssignments.AsNoTracking().AnyAsync(x =>
            x.SchoolId == schoolId
            && x.AcademicSessionId == sheet.Exam.AcademicSessionId
            && x.SchoolClassId == sheet.ExamSubject.SchoolClassId
            && x.SubjectId == sheet.ExamSubject.SubjectId
            && x.TeacherUserId == userId
            && x.IsActive
            && (x.SectionId == null || x.SectionId == sheet.SectionId), cancellationToken);

        return allowed ? (sheet, false) : null;
    }

    private async Task<ExamMarksSheet?> LoadManagerSheetAsync(int schoolId, int sheetId, CancellationToken cancellationToken)
        => await _db.ExamMarksSheets
            .Include(x => x.Exam)
            .Include(x => x.ExamSubject)
            .FirstOrDefaultAsync(x => x.Id == sheetId && x.SchoolId == schoolId, cancellationToken);

    private async Task<int> ExpectedStudentCountAsync(ExamMarksSheet sheet, CancellationToken cancellationToken)
    {
        var query = _db.StudentEnrollments.AsNoTracking().Where(x =>
            x.SchoolId == sheet.SchoolId
            && x.AcademicSessionId == sheet.Exam.AcademicSessionId
            && x.SchoolClassId == sheet.ExamSubject.SchoolClassId
            && x.Status == StudentEnrollmentStatus.Active
            && x.Student.Status == StudentStatus.Active);
        query = query.Where(x => x.SectionId == sheet.SectionId);
        return await query.CountAsync(cancellationToken);
    }

    private static MarkValidationResult ValidateAndCalculate(ExamSubject subject, MarkEntryRowViewModel row)
    {
        var hasTheoryComponent = subject.TheoryMaxMarks.HasValue && subject.TheoryMaxMarks.Value > 0;
        var hasPracticalComponent = subject.PracticalMaxMarks.HasValue && subject.PracticalMaxMarks.Value > 0;

        if (!hasTheoryComponent && !hasPracticalComponent)
        {
            if (!row.TheoryMarks.HasValue) return new(false, "Enter obtained marks.");
            if (row.TheoryMarks < 0 || row.TheoryMarks > subject.MaxMarks)
                return new(false, $"Marks must be between 0 and {subject.MaxMarks:0.##}.");
            return new(true, "", row.TheoryMarks, null, row.TheoryMarks);
        }

        decimal theory = 0m;
        decimal practical = 0m;
        if (hasTheoryComponent)
        {
            if (!row.TheoryMarks.HasValue) return new(false, "Enter theory marks.");
            if (row.TheoryMarks < 0 || row.TheoryMarks > subject.TheoryMaxMarks)
                return new(false, $"Theory marks must be between 0 and {subject.TheoryMaxMarks:0.##}.");
            theory = row.TheoryMarks.Value;
        }
        if (hasPracticalComponent)
        {
            if (!row.PracticalMarks.HasValue) return new(false, "Enter practical marks.");
            if (row.PracticalMarks < 0 || row.PracticalMarks > subject.PracticalMaxMarks)
                return new(false, $"Practical marks must be between 0 and {subject.PracticalMaxMarks:0.##}.");
            practical = row.PracticalMarks.Value;
        }

        var total = theory + practical;
        if (total > subject.MaxMarks)
            return new(false, $"Combined marks cannot exceed {subject.MaxMarks:0.##}.");
        return new(true, "", hasTheoryComponent ? theory : null, hasPracticalComponent ? practical : null, total);
    }

    private record MarkValidationResult(bool Success, string Message, decimal? Theory = null, decimal? Practical = null, decimal? Total = null);
}
