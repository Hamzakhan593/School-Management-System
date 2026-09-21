using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class AcademicSession
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Range(1, 7)]
    public int WorkingDaysPerWeek { get; set; } = 6;

    public AcademicSessionStatus Status { get; set; } = AcademicSessionStatus.Draft;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ICollection<Term> Terms { get; set; } = new List<Term>();
    public ICollection<SchoolHoliday> Holidays { get; set; } = new List<SchoolHoliday>();
    public ICollection<GradingScheme> GradingSchemes { get; set; } = new List<GradingScheme>();
}
