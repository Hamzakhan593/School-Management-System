using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StudentEnrollment
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int AcademicSessionId { get; set; }

    // M05 normalized links. Nullable so M03/M04 historical rows can be migrated safely.
    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }
    public int? AcademicGroupId { get; set; }

    // Snapshot values are intentionally retained so old history remains readable even if
    // a class/section/group is later renamed or deactivated.
    [Required, StringLength(80)]
    public string ClassName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? SectionName { get; set; }

    [StringLength(80)]
    public string? GroupStream { get; set; }

    [StringLength(40)]
    public string? RollNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EffectiveTo { get; set; }

    public bool IsCurrent { get; set; } = true;
    public StudentEnrollmentStatus Status { get; set; } = StudentEnrollmentStatus.Active;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public SchoolClass? SchoolClass { get; set; }
    public Section? Section { get; set; }
    public AcademicGroup? AcademicGroup { get; set; }
}
