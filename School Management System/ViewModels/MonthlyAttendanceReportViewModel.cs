using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class MonthlyAttendanceReportViewModel
{
    public int? AcademicSessionId { get; set; }
    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }
    public DateTime Month { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public decimal LowAttendanceThresholdPercent { get; set; } = 75m;
    public string? AccessMessage { get; set; }

    public string? SelectedSessionName { get; set; }
    public string? SelectedClassName { get; set; }
    public string? SelectedSectionName { get; set; }

    public List<AcademicSession> Sessions { get; set; } = [];
    public List<SchoolClass> Classes { get; set; } = [];
    public List<Section> Sections { get; set; } = [];
    public List<MonthlyAttendanceStudentRowViewModel> Students { get; set; } = [];
}

public class MonthlyAttendanceStudentRowViewModel
{
    public int StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int MarkedDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int LeaveDays { get; set; }
    public int HalfDays { get; set; }
    public decimal AttendancePercentage { get; set; }
    public bool IsLowAttendance { get; set; }
}
