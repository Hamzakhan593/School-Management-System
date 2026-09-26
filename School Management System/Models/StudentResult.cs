using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StudentResult
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public int StudentEnrollmentId { get; set; }

    public int VersionNumber { get; set; } = 1;
    public StudentResultStatus Status { get; set; } = StudentResultStatus.Published;
    public bool IsCurrent { get; set; } = true;

    public decimal ObtainedMarks { get; set; }
    public decimal MaximumMarks { get; set; }
    public decimal Percentage { get; set; }

    [Required, StringLength(20)]
    public string Grade { get; set; } = string.Empty;

    public bool IsPassed { get; set; }
    public int? ClassPosition { get; set; }
    public decimal? AttendancePercentage { get; set; }

    [StringLength(500)]
    public string? TeacherRemarks { get; set; }

    [StringLength(500)]
    public string? CorrectionReason { get; set; }

    [StringLength(450)]
    public string? PublishedByUserId { get; set; }

    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public Exam Exam { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment StudentEnrollment { get; set; } = null!;
    public ApplicationUser? PublishedByUser { get; set; }
}
