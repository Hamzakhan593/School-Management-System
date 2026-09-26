namespace School_Management_System.Services;

public interface ICommunicationFileService
{
    Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveAsync(
        IFormFile file,
        int schoolId,
        string category,
        CancellationToken cancellationToken = default);

    Task<(Stream? Stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
