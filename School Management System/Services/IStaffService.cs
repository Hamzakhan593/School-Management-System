using School_Management_System.Models;

namespace School_Management_System.Services;

public interface IStaffService
{
    Task<(bool Success, string Message, int? StaffId, string? EmployeeId)> CreateAsync(
        Staff staff,
        CancellationToken cancellationToken = default);
}
