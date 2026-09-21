using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class SchoolHoliday
{
    public int Id { get; set; }
    public int AcademicSessionId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    public AcademicSession AcademicSession { get; set; } = null!;
}
