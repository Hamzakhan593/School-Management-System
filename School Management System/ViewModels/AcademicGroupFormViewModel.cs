using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class AcademicGroupFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a class.")]
    [Display(Name = "Class")]
    public int SchoolClassId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
