using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AcademicSessionSetupViewModel
{
    public AcademicSession Session { get; set; } = null!;
    public TermCreateViewModel NewTerm { get; set; } = new();
    public HolidayCreateViewModel NewHoliday { get; set; } = new();
    public GradeRuleCreateViewModel NewGradeRule { get; set; } = new();
}
