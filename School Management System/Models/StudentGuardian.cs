namespace School_Management_System.Models;

public class StudentGuardian
{
    public int StudentId { get; set; }
    public int GuardianId { get; set; }
    public bool IsPrimary { get; set; } = true;

    public Student Student { get; set; } = null!;
    public Guardian Guardian { get; set; } = null!;
}
