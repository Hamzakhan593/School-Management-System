using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StudentMark
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ExamId { get; set; }
    public int ExamSubjectId { get; set; }
    public int ExamMarksSheetId { get; set; }
    public int StudentId { get; set; }
    public int StudentEnrollmentId { get; set; }

    public MarkSpecialStatus SpecialStatus { get; set; } = MarkSpecialStatus.None;

    [Range(typeof(decimal), "0", "100000")]
    public decimal? TheoryMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? PracticalMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? ObtainedMarks { get; set; }

    [StringLength(500)]
    public string? TeacherRemarks { get; set; }

    [StringLength(450)]
    public string? EnteredByUserId { get; set; }

    public DateTime? EnteredAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public Exam Exam { get; set; } = null!;
    public ExamSubject ExamSubject { get; set; } = null!;
    public ExamMarksSheet ExamMarksSheet { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment StudentEnrollment { get; set; } = null!;
}
