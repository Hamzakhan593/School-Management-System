using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class StudentEnrollmentFormViewModel
{
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an academic session.")]
    [Display(Name = "Academic Session")]
    public int AcademicSessionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a class.")]
    [Display(Name = "Class")]
    public int SchoolClassId { get; set; }

    [Display(Name = "Section")]
    public int? SectionId { get; set; }

    [Display(Name = "Group / Stream")]
    public int? AcademicGroupId { get; set; }

    [StringLength(40)]
    [Display(Name = "Roll Number")]
    public string? RollNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Effective From")]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    public StudentEnrollmentStatus Status { get; set; } = StudentEnrollmentStatus.Active;

    [StringLength(500)]
    public string? Notes { get; set; }
}
