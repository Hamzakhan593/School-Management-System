using School_Management_System.Models;

namespace School_Management_System.Services;

public interface ISystemSettingsService
{
    Task<SystemSetting> GetAsync(int schoolId, CancellationToken cancellationToken = default);
    Task<SystemSetting> GetTrackedAsync(int schoolId, CancellationToken cancellationToken = default);
}
