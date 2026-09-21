using School_Management_System.Models;

namespace School_Management_System.Services;

public interface IAcademicSessionService
{
    Task<(bool Success, string Message)> ActivateAsync(int sessionId, int schoolId);
    Task<(bool Success, string Message)> CloseAsync(int sessionId, int schoolId);
    Task<(bool Success, string Message)> ArchiveAsync(int sessionId, int schoolId);
}
