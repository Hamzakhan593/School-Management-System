using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class ResultService : IResultService
{
    private readonly ApplicationDbContext _db;

    public ResultService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StudentExamCalculation?> CalculateStudentExamAsync(
        int schoolId,
        int examId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var examExists = await _db.Exams.AsNoTracking()
            .AnyAsync(x => x.Id == examId && x.SchoolId == schoolId, cancellationToken);
        if (!examExists) return null;

        var marks = await _db.StudentMarks.AsNoTracking()
            .Include(x => x.ExamSubject)
            .Where(x => x.SchoolId == schoolId && x.ExamId == examId && x.StudentId == studentId)
            .ToListAsync(cancellationToken);

        if (marks.Count == 0) return null;

        decimal obtained = 0m;
        decimal maximum = 0m;
        var passed = true;
        var counted = 0;
        var exempt = 0;
        var absent = 0;

        foreach (var mark in marks)
        {
            if (mark.SpecialStatus == MarkSpecialStatus.Exempt)
            {
                exempt++;
                continue;
            }

            var factor = mark.ExamSubject.WeightagePercent / 100m;
            maximum += mark.ExamSubject.MaxMarks * factor;
            counted++;

            if (mark.SpecialStatus == MarkSpecialStatus.Absent)
            {
                absent++;
                passed = false;
                continue;
            }

            var subjectObtained = mark.ObtainedMarks ?? 0m;
            obtained += subjectObtained * factor;
            if (subjectObtained < mark.ExamSubject.PassMarks)
                passed = false;
        }

        if (counted == 0) passed = false;
        var percentage = maximum <= 0 ? 0 : Math.Round(obtained / maximum * 100m, 2, MidpointRounding.AwayFromZero);
        return new StudentExamCalculation(examId, studentId, obtained, maximum, percentage, passed, counted, exempt, absent);
    }

    public async Task<string> ResolveGradeAsync(int academicSessionId, decimal percentage, bool passed, CancellationToken cancellationToken = default)
    {
        if (!passed) return "F";

        var scheme = await _db.GradingSchemes.AsNoTracking()
            .Include(x => x.Rules)
            .Where(x => x.AcademicSessionId == academicSessionId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var rule = scheme?.Rules
            .OrderByDescending(x => x.MinPercentage)
            .FirstOrDefault(x => percentage >= x.MinPercentage && percentage <= x.MaxPercentage);
        return rule?.Grade ?? DefaultGrade(percentage);
    }

    public async Task<decimal?> CalculateAttendancePercentageAsync(
        int schoolId,
        int academicSessionId,
        int studentId,
        DateTime upToDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.StudentAttendances.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == academicSessionId
                && x.StudentId == studentId
                && x.AttendanceDate <= upToDate.Date
                && x.Status != StudentAttendanceStatus.Holiday
                && x.Status != StudentAttendanceStatus.NoClass)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return null;

        decimal attended = 0m;
        foreach (var status in rows)
        {
            attended += status switch
            {
                StudentAttendanceStatus.Present => 1m,
                StudentAttendanceStatus.Late => 1m,
                StudentAttendanceStatus.HalfDay => 0.5m,
                _ => 0m
            };
        }

        return Math.Round(attended / rows.Count * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<IReadOnlyList<StudentResult>> PublishClassResultsAsync(
        int schoolId,
        string userId,
        int examId,
        int classId,
        int? sectionId,
        string? correctionReason,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams
            .Include(x => x.ExamSubjects)
            .FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == schoolId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        if (exam.Status != ExamStatus.MarksLocked && exam.Status != ExamStatus.Published)
            throw new InvalidOperationException("All marks must be locked before results can be published.");

        var examSubjectIds = exam.ExamSubjects.Where(x => x.IsActive && x.SchoolClassId == classId).Select(x => x.Id).ToList();
        if (examSubjectIds.Count == 0)
            throw new InvalidOperationException("No exam subjects are configured for this class.");

        var expectedSections = await _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == exam.AcademicSessionId
                && x.SchoolClassId == classId
                && x.Status == StudentEnrollmentStatus.Active
                && x.Student.Status == StudentStatus.Active
                && (!sectionId.HasValue || x.SectionId == sectionId.Value))
            .Select(x => x.SectionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var subjectId in examSubjectIds)
        {
            foreach (var secId in expectedSections)
            {
                var locked = await _db.ExamMarksSheets.AsNoTracking().AnyAsync(x => x.ExamId == examId
                    && x.ExamSubjectId == subjectId
                    && x.SectionId == secId
                    && x.Status == ExamMarksSheetStatus.Locked, cancellationToken);
                if (!locked)
                    throw new InvalidOperationException("Every subject marks sheet for the selected class/section must be locked before publishing.");
            }
        }

        var enrollments = await _db.StudentEnrollments
            .Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == exam.AcademicSessionId
                && x.SchoolClassId == classId
                && x.Status == StudentEnrollmentStatus.Active
                && x.Student.Status == StudentStatus.Active
                && (!sectionId.HasValue || x.SectionId == sectionId.Value))
            .OrderBy(x => x.RollNumber)
            .ThenBy(x => x.Student.FullName)
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
            throw new InvalidOperationException("No active students were found for the selected class/section.");

        var selectedStudentIds = enrollments.Select(e => e.StudentId).ToList();
        var existingCurrent = await _db.StudentResults
            .Where(x => x.SchoolId == schoolId && x.ExamId == examId && x.IsCurrent && selectedStudentIds.Contains(x.StudentId))
            .ToListAsync(cancellationToken);

        if (existingCurrent.Count > 0 && string.IsNullOrWhiteSpace(correctionReason))
            throw new InvalidOperationException("These results are already published. A correction reason is required to create a new version.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var created = new List<StudentResult>();

        foreach (var enrollment in enrollments)
        {
            var calc = await CalculateStudentExamAsync(schoolId, examId, enrollment.StudentId, cancellationToken);
            if (calc is null)
                throw new InvalidOperationException($"Marks are incomplete for {enrollment.Student.FullName}.");

            var grade = await ResolveGradeAsync(exam.AcademicSessionId, calc.Percentage, calc.Passed, cancellationToken);
            var attendance = await CalculateAttendancePercentageAsync(schoolId, exam.AcademicSessionId, enrollment.StudentId, exam.EndDate, cancellationToken);
            var old = existingCurrent.FirstOrDefault(x => x.StudentId == enrollment.StudentId);
            var nextVersion = old is null ? 1 : old.VersionNumber + 1;

            if (old is not null)
            {
                old.IsCurrent = false;
                old.Status = StudentResultStatus.Superseded;
            }

            var subjectRemarks = await _db.StudentMarks.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.ExamId == examId && x.StudentId == enrollment.StudentId && !string.IsNullOrWhiteSpace(x.TeacherRemarks))
                .Select(x => x.TeacherRemarks!)
                .Distinct()
                .Take(3)
                .ToListAsync(cancellationToken);

            var result = new StudentResult
            {
                SchoolId = schoolId,
                AcademicSessionId = exam.AcademicSessionId,
                ExamId = examId,
                StudentId = enrollment.StudentId,
                StudentEnrollmentId = enrollment.Id,
                VersionNumber = nextVersion,
                Status = StudentResultStatus.Published,
                IsCurrent = true,
                ObtainedMarks = calc.ObtainedMarks,
                MaximumMarks = calc.MaximumMarks,
                Percentage = calc.Percentage,
                Grade = grade,
                IsPassed = calc.Passed,
                AttendancePercentage = attendance,
                TeacherRemarks = subjectRemarks.Count == 0 ? null : string.Join("; ", subjectRemarks),
                CorrectionReason = string.IsNullOrWhiteSpace(correctionReason) ? null : correctionReason.Trim(),
                PublishedByUserId = userId,
                PublishedAtUtc = DateTime.UtcNow
            };
            _db.StudentResults.Add(result);
            created.Add(result);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var ranked = created
            .OrderByDescending(x => x.Percentage)
            .ThenByDescending(x => x.ObtainedMarks)
            .ThenBy(x => x.StudentId)
            .ToList();

        int position = 0;
        decimal? lastPercentage = null;
        decimal? lastObtained = null;
        for (var i = 0; i < ranked.Count; i++)
        {
            if (lastPercentage != ranked[i].Percentage || lastObtained != ranked[i].ObtainedMarks)
                position = i + 1;
            ranked[i].ClassPosition = position;
            lastPercentage = ranked[i].Percentage;
            lastObtained = ranked[i].ObtainedMarks;
        }

        exam.Status = ExamStatus.Published;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    private static string DefaultGrade(decimal percentage)
        => percentage >= 80 ? "A+"
            : percentage >= 70 ? "A"
            : percentage >= 60 ? "B"
            : percentage >= 50 ? "C"
            : percentage >= 40 ? "D"
            : "F";
}
