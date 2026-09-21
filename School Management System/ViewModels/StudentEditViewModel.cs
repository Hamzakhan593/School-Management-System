using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class StudentEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Student Name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Father / Guardian Name")]
    public string? FatherGuardianName { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }

    [StringLength(30)]
    [Display(Name = "B-Form / CNIC")]
    public string? BFormCnic { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "Contact Number")]
    public string? ContactNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Admission Date")]
    public DateTime AdmissionDate { get; set; }

    public StudentStatus Status { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
