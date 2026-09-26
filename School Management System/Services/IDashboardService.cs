using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> BuildAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default);
}
