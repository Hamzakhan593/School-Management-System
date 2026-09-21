using Microsoft.AspNetCore.Http;

namespace School_Management_System.Services;

public class AdmissionFileService : IAdmissionFileService
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private readonly IWebHostEnvironment _environment;

    public AdmissionFileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveAsync(
        IFormFile file, int schoolId, int applicationId, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            return (false, "The selected file is empty.", null, null, 0);

        if (file.Length > MaxBytes)
            return (false, "Document must be 5 MB or smaller.", null, null, 0);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (false, "Allowed document types: PDF, JPG, JPEG and PNG.", null, null, 0);

        var basePath = Path.Combine(_environment.ContentRootPath, "App_Data", "AdmissionDocuments");
        var folder = Path.Combine(basePath, schoolId.ToString(), applicationId.ToString());
        Directory.CreateDirectory(folder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var diskPath = Path.Combine(folder, storedFileName);
        await using var output = File.Create(diskPath);
        await file.CopyToAsync(output, cancellationToken);

        var relative = Path.Combine(schoolId.ToString(), applicationId.ToString(), storedFileName).Replace('\\', '/');
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
        return (true, "Document uploaded.", relative, contentType, file.Length);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var normalized = storageKey.Replace('/', Path.DirectorySeparatorChar);
        if (normalized.Contains("..", StringComparison.Ordinal))
            return Task.FromResult<Stream?>(null);

        var basePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "AdmissionDocuments"));
        var diskPath = Path.GetFullPath(Path.Combine(basePath, normalized));
        if (!diskPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) || !File.Exists(diskPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }
}
