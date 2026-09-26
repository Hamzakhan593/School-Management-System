using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Exam
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int? TermId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string ExamType { get; set; } = "Term Exam";

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public bool IsActive { get; set; } = true;

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public Term? Term { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
    public ICollection<ExamSubject> ExamSubjects { get; set; } = new List<ExamSubject>();
}
