using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class CameraFaceEnrollment
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }

    [Required, StringLength(100)]
    public string ProviderName { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string TemplateReference { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? EnrolledByUserId { get; set; }

    public School School { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
