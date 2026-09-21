using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class AuditLog
{
    public long Id { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(160)]
    public string? UserEmail { get; set; }

    [Required, StringLength(120)]
    public string Action { get; set; } = string.Empty;

    [StringLength(120)]
    public string EntityType { get; set; } = string.Empty;

    [StringLength(450)]
    public string? EntityId { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
