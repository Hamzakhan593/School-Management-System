namespace School_Management_System.Services;

public interface IAuditService
{
    Task WriteAsync(string action, string entityType = "", string? entityId = null, string? details = null,
        string? oldValues = null, string? newValues = null);
}
