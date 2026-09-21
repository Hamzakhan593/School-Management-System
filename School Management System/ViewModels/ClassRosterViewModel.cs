using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class ClassRosterViewModel
{
    public SchoolClass SchoolClass { get; set; } = null!;
    public Section? Section { get; set; }
    public AcademicSession AcademicSession { get; set; } = null!;
    public IReadOnlyList<StudentEnrollment> Enrollments { get; set; } = [];
}
