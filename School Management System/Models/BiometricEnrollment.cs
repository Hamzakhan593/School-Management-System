using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class BiometricEnrollment
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int BiometricDeviceId { get; set; }
    public int StudentId { get; set; }

    [Required, StringLength(100)]
    public string DeviceUserReference { get; set; } = string.Empty;

    public BiometricModality Modality { get; set; } = BiometricModality.Fingerprint;

    // Optional reference to a template held by the device/provider. The template itself is not stored here.
    [StringLength(200)]
    public string? TemplateReference { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? EnrolledByUserId { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public BiometricDevice BiometricDevice { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
