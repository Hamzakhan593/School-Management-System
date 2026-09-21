using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Student
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(40)]
    public string AdmissionNumber { get; set; } = string.Empty;

    // Kept as a quick current-roll reference for search/list screens.
    // Historical roll numbers are preserved in StudentEnrollment.
    [StringLength(40)]
    public string? RollNumber { get; set; }

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? FatherGuardianName { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [StringLength(30)]
    public string? BFormCnic { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? ContactNumber { get; set; }

    [StringLength(500)]
    public string? PhotoStorageKey { get; set; }

    [DataType(DataType.Date)]
    public DateTime AdmissionDate { get; set; }

    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
    public ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
}
