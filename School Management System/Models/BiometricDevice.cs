using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class BiometricDevice
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(80)]
    public string DeviceCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Model { get; set; }

    [StringLength(80)]
    public string? ConnectionType { get; set; }

    [StringLength(80)]
    public string? IpAddress { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    // Only a SHA-256 hash of the device integration key is stored.
    [Required, StringLength(128)]
    public string ApiKeyHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncAtUtc { get; set; }

    [StringLength(300)]
    public string? LastStatusMessage { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ICollection<BiometricEnrollment> Enrollments { get; set; } = new List<BiometricEnrollment>();
    public ICollection<AttendanceEvent> AttendanceEvents { get; set; } = new List<AttendanceEvent>();
}
