using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IPromotionService
{
    Task<PromotionIndexViewModel> GetIndexAsync(int schoolId);
    Task<RolloverCreateViewModel> BuildCreateModelAsync(int schoolId, int? sourceSessionId = null, RolloverCreateViewModel? current = null);
    Task<(bool Success, string Message, int? BatchId)> CreateRolloverAsync(int schoolId, string? userId, RolloverCreateViewModel model);
    Task<(bool Success, string Message)> PreparePreviewAsync(int schoolId, int batchId);
    Task<PromotionPreviewViewModel?> GetPreviewAsync(int schoolId, int batchId);
    Task<(bool Success, string Message)> SavePreviewAsync(int schoolId, PromotionPreviewPostViewModel model);
    Task<(bool Success, string Message)> CommitAsync(int schoolId, int batchId);
    Task<(bool Success, string Message)> RollbackAsync(int schoolId, int batchId);
    Task<(bool Success, string Message)> FinalizeAsync(int schoolId, int batchId);
}
