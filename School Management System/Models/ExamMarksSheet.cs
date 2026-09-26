using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class ExamMarksSheet
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ExamId { get; set; }
    public int ExamSubjectId { get; set; }
    public int? SectionId { get; set; }
    public ExamMarksSheetStatus Status { get; set; } = ExamMarksSheetStatus.Draft;

    [StringLength(450)] public string? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    [StringLength(450)] public string? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    [StringLength(450)] public string? LockedByUserId { get; set; }
    public DateTime? LockedAtUtc { get; set; }
    [StringLength(450)] public string? ReopenedByUserId { get; set; }
    public DateTime? ReopenedAtUtc { get; set; }
    [StringLength(500)] public string? ReopenReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public School School { get; set; } = null!;
    public Exam Exam { get; set; } = null!;
    public ExamSubject ExamSubject { get; set; } = null!;
    public Section? Section { get; set; }
    public ICollection<StudentMark> StudentMarks { get; set; } = new List<StudentMark>();
}
