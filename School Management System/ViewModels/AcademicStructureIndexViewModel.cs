using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AcademicStructureIndexViewModel
{
    public int? SelectedSessionId { get; set; }
    public IReadOnlyList<AcademicSession> Sessions { get; set; } = [];
    public IReadOnlyList<SchoolClass> Classes { get; set; } = [];
    public IReadOnlyList<Subject> Subjects { get; set; } = [];
    public IReadOnlyList<ClassSubject> ClassSubjects { get; set; } = [];
    public IReadOnlyList<TeacherAssignment> TeacherAssignments { get; set; } = [];
    public IReadOnlyList<ApplicationUser> Teachers { get; set; } = [];
    public int LegacyPlacementsUnmapped { get; set; }
}
