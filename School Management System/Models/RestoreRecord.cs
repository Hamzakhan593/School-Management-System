using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class RestoreRecord
{
    public long Id { get; set; }
    public int SchoolId { get; set; }
    public long? BackupRecordId { get; set; }

    [Required, StringLength(260)]
    public string BackupFileName { get; set; } = string.Empty;

    [StringLength(64)]
    public string? BackupSha256 { get; set; }

    [StringLength(260)]
    public string? SafetyBackupFileName { get; set; }

    [Required, StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(450)]
    public string? RequestedByUserId { get; set; }

    [StringLength(160)]
    public string? RequestedByEmail { get; set; }

    public bool WasSuccessful { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    public School School { get; set; } = null!;
}
