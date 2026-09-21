using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class TeacherAssignment
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int SchoolClassId { get; set; }
    public int? SectionId { get; set; }
    public int SubjectId { get; set; }
    public string TeacherUserId { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public SchoolClass SchoolClass { get; set; } = null!;
    public Section? Section { get; set; }
    public Subject Subject { get; set; } = null!;
    public ApplicationUser TeacherUser { get; set; } = null!;
}
