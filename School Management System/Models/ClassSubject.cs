using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class ClassSubject
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int SchoolClassId { get; set; }
    public int SubjectId { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? MaxMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? PassMarks { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public SchoolClass SchoolClass { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
