using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StudentAttendance
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int StudentId { get; set; }
    public int StudentEnrollmentId { get; set; }
    public int SchoolClassId { get; set; }
    public int? SectionId { get; set; }

    [DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; }

    public StudentAttendanceStatus Status { get; set; } = StudentAttendanceStatus.Present;
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;

    [StringLength(300)]
    public string? Remarks { get; set; }

    [StringLength(450)]
    public string? MarkedByUserId { get; set; }

    public DateTime MarkedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? LastModifiedByUserId { get; set; }

    public DateTime? LastModifiedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment StudentEnrollment { get; set; } = null!;
    public SchoolClass SchoolClass { get; set; } = null!;
    public Section? Section { get; set; }
}
