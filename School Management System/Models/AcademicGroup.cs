using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class AcademicGroup
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int SchoolClassId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public School School { get; set; } = null!;
    public SchoolClass SchoolClass { get; set; } = null!;
}
