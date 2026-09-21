using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class AdmissionApplication
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int? AdmissionEnquiryId { get; set; }
    public int? StudentId { get; set; }

    [Required, StringLength(150)]
    public string StudentName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Gender { get; set; }

    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [StringLength(30)]
    public string? BFormCnic { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? StudentContactNumber { get; set; }

    [Required, StringLength(80)]
    public string DesiredClass { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    [StringLength(180)]
    public string? PreviousSchoolName { get; set; }

    [StringLength(80)]
    public string? PreviousClass { get; set; }

    [StringLength(500)]
    public string? PreviousResultSummary { get; set; }

    [Required, StringLength(150)]
    public string GuardianName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string GuardianRelationship { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string GuardianPhone { get; set; } = string.Empty;

    [StringLength(120)]
    public string? GuardianOccupation { get; set; }

    [StringLength(30)]
    public string? GuardianCnic { get; set; }

    [StringLength(500)]
    public string? GuardianAddress { get; set; }

    [StringLength(150)]
    public string? EmergencyContactName { get; set; }

    [StringLength(60)]
    public string? EmergencyContactRelationship { get; set; }

    [StringLength(50)]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(1200)]
    public string? Notes { get; set; }

    public AdmissionApplicationStatus Status { get; set; } = AdmissionApplicationStatus.Draft;

    [StringLength(40)]
    public string? AdmissionNumber { get; set; }

    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? AdmittedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public AdmissionEnquiry? AdmissionEnquiry { get; set; }
    public Student? Student { get; set; }
    public ICollection<AdmissionDocument> Documents { get; set; } = new List<AdmissionDocument>();
}
