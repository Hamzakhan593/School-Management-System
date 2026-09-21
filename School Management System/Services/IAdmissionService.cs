namespace School_Management_System.Services;

public interface IAdmissionService
{
    Task<(bool Success, string Message, int? StudentId, string? AdmissionNumber)> AdmitAsync(
        int applicationId, int schoolId, string? userId, CancellationToken cancellationToken = default);
}
