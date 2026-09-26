using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IExamService
{
    Task<MarksEntryViewModel?> BuildMarksEntryAsync(
        int schoolId,
        string userId,
        bool isExamManager,
        int examSubjectId,
        int? sectionId,
        CancellationToken cancellationToken = default);

    Task<ExamOperationResult> SaveMarksAsync(
        int schoolId,
        string userId,
        bool isExamManager,
        MarksEntryViewModel model,
        CancellationToken cancellationToken = default);

    Task<ExamOperationResult> SubmitMarksAsync(int schoolId, string userId, bool isExamManager, int sheetId, CancellationToken cancellationToken = default);
    Task<ExamOperationResult> VerifyMarksAsync(int schoolId, string userId, int sheetId, CancellationToken cancellationToken = default);
    Task<ExamOperationResult> LockMarksAsync(int schoolId, string userId, int sheetId, CancellationToken cancellationToken = default);
    Task<ExamOperationResult> ReopenMarksAsync(int schoolId, string userId, int sheetId, string reason, CancellationToken cancellationToken = default);
}

public record ExamOperationResult(bool Success, string Message, int? EntityId = null);
