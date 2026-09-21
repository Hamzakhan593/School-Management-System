using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class SubjectFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Theory")]
    public bool HasTheory { get; set; } = true;

    [Display(Name = "Practical")]
    public bool HasPractical { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Default max marks")]
    public decimal? DefaultMaxMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Default pass marks")]
    public decimal? DefaultPassMarks { get; set; }

    public bool IsActive { get; set; } = true;
}
