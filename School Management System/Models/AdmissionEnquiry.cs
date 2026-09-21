using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class AdmissionEnquiry
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int? AcademicSessionId { get; set; }

    [Required, StringLength(150)]
    public string StudentName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string ParentGuardianName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ContactNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [Required, StringLength(80)]
    public string DesiredClass { get; set; } = string.Empty;

    [StringLength(120)]
    public string? SourceReferral { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FollowUpDate { get; set; }

    public AdmissionEnquiryStage Stage { get; set; } = AdmissionEnquiryStage.New;

    [StringLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession? AcademicSession { get; set; }
    public AdmissionApplication? AdmissionApplication { get; set; }
}
