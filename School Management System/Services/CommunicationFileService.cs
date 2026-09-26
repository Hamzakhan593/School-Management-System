namespace School_Management_System.Services;

public class CommunicationFileService : ICommunicationFileService
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png"
    };

    private readonly IWebHostEnvironment _environment;

    public CommunicationFileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<(bool Success, string Message, string? StorageKey, string? ContentType, long SizeBytes)> SaveAsync(
        IFormFile file,
        int schoolId,
        string category,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            return (false, "The selected file is empty.", null, null, 0);

        if (file.Length > MaxBytes)
            return (false, "File must be 10 MB or smaller.", null, null, 0);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (false, "Allowed file types: PDF, DOC, DOCX, XLS, XLSX, JPG, JPEG and PNG.", null, null, 0);

        var safeCategory = category.Equals("notice", StringComparison.OrdinalIgnoreCase) ? "notices" : "documents";
        var basePath = Path.Combine(_environment.ContentRootPath, "App_Data", "Communications");
        var folder = Path.Combine(basePath, schoolId.ToString(), safeCategory);
        Directory.CreateDirectory(folder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var diskPath = Path.Combine(folder, storedFileName);
        await using var output = File.Create(diskPath);
        await file.CopyToAsync(output, cancellationToken);

        var relative = Path.Combine(schoolId.ToString(), safeCategory, storedFileName).Replace('\\', '/');
        return (true, "File uploaded.", relative, GetContentType(extension), file.Length);
    }

    public Task<(Stream? Stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsSafeRelativePath(storageKey))
            return Task.FromResult<(Stream?, string)>((null, "application/octet-stream"));

        var basePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "Communications"));
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

        var basePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "Communications"));
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
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
