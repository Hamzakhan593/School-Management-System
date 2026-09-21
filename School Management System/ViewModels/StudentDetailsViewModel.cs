using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class StudentDetailsViewModel
{
    public Student Student { get; set; } = null!;
    public StudentEnrollment? CurrentEnrollment { get; set; }
    public IReadOnlyList<StudentEnrollment> EnrollmentHistory { get; set; } = [];
    public IReadOnlyList<StudentGuardian> Guardians { get; set; } = [];
    public IReadOnlyList<StudentDocument> Documents { get; set; } = [];
}
