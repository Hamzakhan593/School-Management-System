using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class StudentGuardianFormViewModel
{
    public int StudentId { get; set; }
    public int GuardianId { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Guardian Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Relationship { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Occupation { get; set; }

    [StringLength(30)]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Primary Guardian")]
    public bool IsPrimary { get; set; }
}
