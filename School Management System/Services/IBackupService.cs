using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IBackupService
{
    Task<BackupDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<BackupRecord> CreateBackupAsync(BackupType type, string? notes = null, CancellationToken cancellationToken = default);
    Task<BackupRecord> VerifyAsync(long backupId, CancellationToken cancellationToken = default);
    Task DeleteAsync(long backupId, CancellationToken cancellationToken = default);
    Task RestoreAsync(long backupId, string reason, CancellationToken cancellationToken = default);
    Task CreateScheduledBackupIfDueAsync(CancellationToken cancellationToken = default);
}
