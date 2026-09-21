using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class HolidayCreateViewModel
{
    public int AcademicSessionId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
