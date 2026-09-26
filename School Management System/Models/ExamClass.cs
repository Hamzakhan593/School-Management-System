namespace School_Management_System.Models;

public class ExamClass
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ExamId { get; set; }
    public int SchoolClassId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public School School { get; set; } = null!;
    public Exam Exam { get; set; } = null!;
    public SchoolClass SchoolClass { get; set; } = null!;
}
