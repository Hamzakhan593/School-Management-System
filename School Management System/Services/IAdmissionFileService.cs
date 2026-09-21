using Microsoft.AspNetCore.Http;

namespace School_Management_System.Services;

public interface IAdmissionFileService
{
    Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveAsync(
        IFormFile file, int schoolId, int applicationId, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
