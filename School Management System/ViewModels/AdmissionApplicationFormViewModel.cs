using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class AdmissionApplicationFormViewModel
{
    public int Id { get; set; }
    public int? AdmissionEnquiryId { get; set; }

    [Required, Display(Name = "Academic session")]
    public int AcademicSessionId { get; set; }

    [Required, StringLength(150), Display(Name = "Student name")]
    public string StudentName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Gender { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateTime DateOfBirth { get; set; }

    [StringLength(30), Display(Name = "B-Form / CNIC")]
    public string? BFormCnic { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50), Display(Name = "Student contact")]
    public string? StudentContactNumber { get; set; }

    [Required, StringLength(80), Display(Name = "Desired class")]
    public string DesiredClass { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Admission date")]
    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    [StringLength(180), Display(Name = "Previous school")]
    public string? PreviousSchoolName { get; set; }

    [StringLength(80), Display(Name = "Previous class")]
    public string? PreviousClass { get; set; }

    [StringLength(500), Display(Name = "Previous result / remarks")]
    public string? PreviousResultSummary { get; set; }

    [Required, StringLength(150), Display(Name = "Guardian name")]
    public string GuardianName { get; set; } = string.Empty;

    [Required, StringLength(60), Display(Name = "Relationship")]
    public string GuardianRelationship { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Guardian phone")]
    public string GuardianPhone { get; set; } = string.Empty;

    [StringLength(120), Display(Name = "Guardian occupation")]
    public string? GuardianOccupation { get; set; }

    [StringLength(30), Display(Name = "Guardian CNIC")]
    public string? GuardianCnic { get; set; }

    [StringLength(500), Display(Name = "Guardian address")]
    public string? GuardianAddress { get; set; }

    [StringLength(150), Display(Name = "Emergency contact name")]
    public string? EmergencyContactName { get; set; }

    [StringLength(60), Display(Name = "Emergency relationship")]
    public string? EmergencyContactRelationship { get; set; }

    [StringLength(50), Display(Name = "Emergency phone")]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(1200)]
    public string? Notes { get; set; }
}
