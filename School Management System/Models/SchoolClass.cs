using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class SchoolClass
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Code { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<AcademicGroup> Groups { get; set; } = new List<AcademicGroup>();
    public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
}
