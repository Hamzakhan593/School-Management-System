using Microsoft.AspNetCore.Http;

namespace School_Management_System.Services;

public interface IExpenseFileService
{
    Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveAsync(
        IFormFile file, int schoolId, int expenseId, CancellationToken cancellationToken = default);

    Task<(Stream? Stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
