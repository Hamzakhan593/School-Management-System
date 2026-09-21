using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AdmissionEnquiryFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150), Display(Name = "Student name")]
    public string StudentName { get; set; } = string.Empty;

    [Required, StringLength(150), Display(Name = "Parent / guardian")]
    public string ParentGuardianName { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Contact number")]
    public string ContactNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [Required, StringLength(80), Display(Name = "Desired class")]
    public string DesiredClass { get; set; } = string.Empty;

    [StringLength(120), Display(Name = "Source / referral")]
    public string? SourceReferral { get; set; }

    [DataType(DataType.Date), Display(Name = "Follow-up date")]
    public DateTime? FollowUpDate { get; set; }

    [Display(Name = "Academic session")]
    public int? AcademicSessionId { get; set; }

    public AdmissionEnquiryStage Stage { get; set; } = AdmissionEnquiryStage.New;

    [StringLength(1200)]
    public string? Notes { get; set; }
}
