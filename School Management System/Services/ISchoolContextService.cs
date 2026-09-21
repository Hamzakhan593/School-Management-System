using School_Management_System.Models;

namespace School_Management_System.Services;

public interface ISchoolContextService
{
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<int?> GetCurrentSchoolIdAsync();
    Task<School?> GetCurrentSchoolAsync();
}
