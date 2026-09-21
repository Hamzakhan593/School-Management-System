using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Section
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int SchoolClassId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 5000)]
    public int Capacity { get; set; } = 40;

    [StringLength(80)]
    public string? Classroom { get; set; }

    public string? ClassTeacherUserId { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public SchoolClass SchoolClass { get; set; } = null!;
    public ApplicationUser? ClassTeacherUser { get; set; }
}
