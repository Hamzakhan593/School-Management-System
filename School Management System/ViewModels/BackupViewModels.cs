using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class BackupDashboardViewModel
{
    public BackupRecord? LatestSuccessful { get; set; }
    public BackupRecord? LatestVerified { get; set; }
    public IReadOnlyList<BackupRecord> Backups { get; set; } = Array.Empty<BackupRecord>();
    public IReadOnlyList<RestoreRecord> Restores { get; set; } = Array.Empty<RestoreRecord>();
    public DateTime? NextScheduledLocal { get; set; }
    public bool ScheduledBackupsEnabled { get; set; }
    public int RetentionDays { get; set; }
    public bool AllowInAppRestore { get; set; }
    public string StorageRootDisplay { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public class RestoreBackupViewModel
{
    public long BackupId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsVerified { get; set; }

    [Required, StringLength(1000, MinimumLength = 10)]
    [Display(Name = "Reason for restore")]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm your password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Type RESTORE to confirm")]
    public string ConfirmationText { get; set; } = string.Empty;
}

public class AuditLogIndexViewModel
{
    public IReadOnlyList<AuditLog> Items { get; set; } = Array.Empty<AuditLog>();
    public string? Search { get; set; }
    public string? Action { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
