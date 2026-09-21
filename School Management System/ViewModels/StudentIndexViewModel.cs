using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class StudentIndexViewModel
{
    public IReadOnlyList<Student> Students { get; set; } = [];
    public IReadOnlyList<AcademicSession> Sessions { get; set; } = [];
    public IReadOnlyList<string> Classes { get; set; } = [];
    public string? Query { get; set; }
    public StudentStatus? Status { get; set; }
    public string? ClassName { get; set; }
    public int? AcademicSessionId { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int InactiveStudents { get; set; }
}
