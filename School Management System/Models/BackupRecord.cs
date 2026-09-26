using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class BackupRecord
{
    public long Id { get; set; }
    public int SchoolId { get; set; }
    public BackupType BackupType { get; set; }
    public BackupStatus Status { get; set; } = BackupStatus.InProgress;

    [Required, StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string StorageReference { get; set; } = string.Empty;

    [StringLength(180)]
    public string? DatabaseName { get; set; }

    public long? SizeBytes { get; set; }

    [StringLength(64)]
    public string? Sha256 { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }

    [StringLength(450)]
    public string? RequestedByUserId { get; set; }

    [StringLength(160)]
    public string? RequestedByEmail { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? RetentionUntilUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public School School { get; set; } = null!;
}
