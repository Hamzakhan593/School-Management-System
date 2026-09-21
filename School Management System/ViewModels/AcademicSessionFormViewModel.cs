using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class AcademicSessionFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "Session Name")]
    public string Name { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1).AddDays(-1);

    [Range(1, 7)]
    [Display(Name = "Working Days Per Week")]
    public int WorkingDaysPerWeek { get; set; } = 6;

    [StringLength(500)]
    public string? Notes { get; set; }

    public string? CurrentStatus { get; set; }
}
