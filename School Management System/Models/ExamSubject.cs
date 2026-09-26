using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class ExamSubject
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ExamId { get; set; }
    public int SchoolClassId { get; set; }
    public int SubjectId { get; set; }

    [Range(typeof(decimal), "0.01", "100000")]
    public decimal MaxMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal PassMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? TheoryMaxMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? PracticalMaxMarks { get; set; }

    [Range(typeof(decimal), "0.01", "1000")]
    public decimal WeightagePercent { get; set; } = 100m;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public Exam Exam { get; set; } = null!;
    public SchoolClass SchoolClass { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<ExamMarksSheet> MarksSheets { get; set; } = new List<ExamMarksSheet>();
    public ICollection<StudentMark> StudentMarks { get; set; } = new List<StudentMark>();
}
