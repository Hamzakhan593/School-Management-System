using Microsoft.AspNetCore.Http;

namespace School_Management_System.Services;

public class StaffFileService : IStaffFileService
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };
    private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    private readonly IWebHostEnvironment _environment;

    public StaffFileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveDocumentAsync(
        IFormFile file, int schoolId, int staffId, CancellationToken cancellationToken = default)
        => SaveAsync(file, schoolId, staffId, "documents", DocumentExtensions, "Allowed document types: PDF, JPG, JPEG and PNG.", cancellationToken);

    public Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SavePhotoAsync(
        IFormFile file, int schoolId, int staffId, CancellationToken cancellationToken = default)
        => SaveAsync(file, schoolId, staffId, "photos", PhotoExtensions, "Staff photo must be JPG, JPEG or PNG.", cancellationToken);

    private async Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveAsync(
        IFormFile file,
        int schoolId,
        int staffId,
        string category,
        HashSet<string> allowedExtensions,
        string invalidTypeMessage,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
            return (false, "The selected file is empty.", null, null, 0);

        if (file.Length > MaxBytes)
            return (false, "File must be 5 MB or smaller.", null, null, 0);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return (false, invalidTypeMessage, null, null, 0);

        var basePath = Path.Combine(_environment.ContentRootPath, "App_Data", "StaffFiles");
        var folder = Path.Combine(basePath, schoolId.ToString(), staffId.ToString(), category);
        Directory.CreateDirectory(folder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var diskPath = Path.Combine(folder, storedFileName);
        await using var output = File.Create(diskPath);
        await file.CopyToAsync(output, cancellationToken);

        var relative = Path.Combine(schoolId.ToString(), staffId.ToString(), category, storedFileName).Replace('\\', '/');
        return (true, "File uploaded.", relative, GetContentType(extension), file.Length);
    }

    public Task<(Stream? Stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsSafeRelativePath(storageKey))
            return Task.FromResult<(Stream?, string)>((null, "application/octet-stream"));

        var basePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "StaffFiles"));
        var filePath = Path.GetFullPath(Path.Combine(basePath, storageKey.Replace('/', Path.DirectorySeparatorChar)));

        if (!filePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
            return Task.FromResult<(Stream?, string)>((null, "application/octet-stream"));

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return Task.FromResult<(Stream?, string)>((stream, GetContentType(Path.GetExtension(filePath))));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsSafeRelativePath(storageKey))
            return Task.CompletedTask;

        var basePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "StaffFiles"));
        var filePath = Path.GetFullPath(Path.Combine(basePath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (filePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) && File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    private static bool IsSafeRelativePath(string storageKey)
        => !string.IsNullOrWhiteSpace(storageKey)
           && !Path.IsPathRooted(storageKey)
           && !storageKey.Contains("..", StringComparison.Ordinal);

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
