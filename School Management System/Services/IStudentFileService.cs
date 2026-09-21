using Microsoft.AspNetCore.Http;

namespace School_Management_System.Services;

public interface IStudentFileService
{
    Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveDocumentAsync(
        IFormFile file, int schoolId, int studentId, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SavePhotoAsync(
        IFormFile file, int schoolId, int studentId, CancellationToken cancellationToken = default);

    Task<(Stream? Stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
