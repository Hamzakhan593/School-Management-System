using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class SchoolClassFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Code { get; set; }

    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
