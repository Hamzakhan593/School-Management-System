using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class GradeRuleCreateViewModel
{
    public int AcademicSessionId { get; set; }

    [Required, StringLength(20)]
    public string Grade { get; set; } = string.Empty;

    [Range(0, 100)]
    [Display(Name = "Minimum %")]
    public decimal MinPercentage { get; set; }

    [Range(0, 100)]
    [Display(Name = "Maximum %")]
    public decimal MaxPercentage { get; set; } = 100;

    [StringLength(150)]
    public string? Remarks { get; set; }
}
