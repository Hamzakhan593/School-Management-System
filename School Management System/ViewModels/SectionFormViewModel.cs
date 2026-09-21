using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class SectionFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a class.")]
    [Display(Name = "Class")]
    public int SchoolClassId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 5000)]
    public int Capacity { get; set; } = 40;

    [StringLength(80)]
    public string? Classroom { get; set; }

    [Display(Name = "Class teacher")]
    public string? ClassTeacherUserId { get; set; }

    public bool IsActive { get; set; } = true;
}
