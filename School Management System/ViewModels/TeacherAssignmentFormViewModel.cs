using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class TeacherAssignmentFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an academic session.")]
    [Display(Name = "Academic session")]
    public int AcademicSessionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a class.")]
    [Display(Name = "Class")]
    public int SchoolClassId { get; set; }

    [Display(Name = "Section (optional)")]
    public int? SectionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a subject.")]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    [Required]
    [Display(Name = "Teacher")]
    public string TeacherUserId { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }
}
