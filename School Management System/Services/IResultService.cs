using School_Management_System.Models;

namespace School_Management_System.Services;

public interface IResultService
{
    Task<StudentExamCalculation?> CalculateStudentExamAsync(int schoolId, int examId, int studentId, CancellationToken cancellationToken = default);
    Task<string> ResolveGradeAsync(int academicSessionId, decimal percentage, bool passed, CancellationToken cancellationToken = default);
    Task<decimal?> CalculateAttendancePercentageAsync(int schoolId, int academicSessionId, int studentId, DateTime upToDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentResult>> PublishClassResultsAsync(int schoolId, string userId, int examId, int classId, int? sectionId, string? correctionReason, CancellationToken cancellationToken = default);
}

public record StudentExamCalculation(
    int ExamId,
    int StudentId,
    decimal ObtainedMarks,
    decimal MaximumMarks,
    decimal Percentage,
    bool Passed,
    int CountedSubjects,
    int ExemptSubjects,
    int AbsentSubjects);
