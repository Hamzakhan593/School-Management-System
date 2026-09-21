using Microsoft.AspNetCore.Http;

namespace School_Management_System.Services;

public class StudentFileService : IStudentFileService
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

    public StudentFileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveDocumentAsync(
        IFormFile file, int schoolId, int studentId, CancellationToken cancellationToken = default)
        => SaveAsync(file, schoolId, studentId, "documents", DocumentExtensions, "Allowed document types: PDF, JPG, JPEG and PNG.", cancellationToken);

    public Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SavePhotoAsync(
        IFormFile file, int schoolId, int studentId, CancellationToken cancellationToken = default)
        => SaveAsync(file, schoolId, studentId, "photos", PhotoExtensions, "Student photo must be JPG, JPEG or PNG.", cancellationToken);

    private async Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveAsync(
        IFormFile file,
        int schoolId,
        int studentId,
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

        var basePath = Path.Combine(_environment.ContentRootPath, "App_Data", "StudentFiles");
        var folder = Path.Combine(basePath, schoolId.ToString(), studentId.ToString(), category);
        Directory.CreateDirectory(folder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var diskPath = Path.Combine(folder, storedFileName);
        await using var output = File.Create(diskPath);
        await file.CopyToAsync(output, cancellationToken);

        var relative = Path.Combine(schoolId.ToString(), studentId.ToString(), category, storedFileName).Replace('\\', '/');
        return (true, "File uploaded.", relative, GetContentType(extension), file.Length);
    }

    public Task<(Stream? Stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsSafeRelativePath(storageKey))
            return Task.FromResult<(Stream?, string)>((null, "application/octet-stream"));

        // M04 files.
        var studentBase = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "StudentFiles"));
        var studentPath = Path.GetFullPath(Path.Combine(studentBase, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (studentPath.StartsWith(studentBase, StringComparison.OrdinalIgnoreCase) && File.Exists(studentPath))
        {
            Stream stream = new FileStream(studentPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
            return Task.FromResult<(Stream?, string)>((stream, GetContentType(Path.GetExtension(studentPath))));
        }

        // M03 admission documents were copied into StudentDocument using their original storage key.
        var admissionBase = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "AdmissionDocuments"));
        var admissionPath = Path.GetFullPath(Path.Combine(admissionBase, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (admissionPath.StartsWith(admissionBase, StringComparison.OrdinalIgnoreCase) && File.Exists(admissionPath))
        {
            Stream stream = new FileStream(admissionPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
            return Task.FromResult<(Stream?, string)>((stream, GetContentType(Path.GetExtension(admissionPath))));
        }

        return Task.FromResult<(Stream?, string)>((null, "application/octet-stream"));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsSafeRelativePath(storageKey))
            return Task.CompletedTask;

        var studentBase = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "StudentFiles"));
        var studentPath = Path.GetFullPath(Path.Combine(studentBase, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (studentPath.StartsWith(studentBase, StringComparison.OrdinalIgnoreCase) && File.Exists(studentPath))
            File.Delete(studentPath);

        // Intentionally do not delete M03 AdmissionDocuments here: those files may still be referenced
        // by the original admission application for audit/history purposes.
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
