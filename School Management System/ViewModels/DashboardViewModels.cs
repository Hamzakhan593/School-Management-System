namespace School_Management_System.ViewModels;

public class DashboardViewModel
{
    public string SchoolName { get; set; } = "School Management System";
    public string? SchoolLogoPath { get; set; }
    public string AcademicSessionName { get; set; } = "No active session";
    public string UserName { get; set; } = "User";
    public string RoleName { get; set; } = "User";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public bool HasSchoolContext { get; set; }

    public bool ShowAdmissions { get; set; }
    public bool ShowAttendance { get; set; }
    public bool ShowFinance { get; set; }
    public bool ShowAcademics { get; set; }
    public bool ShowHr { get; set; }
    public bool ShowAudit { get; set; }

    public int ActiveStudents { get; set; }
    public int ActiveStaff { get; set; }
    public int NewAdmissionsThisMonth { get; set; }
    public int OpenEnquiries { get; set; }
    public int PendingAdmissions { get; set; }

    public decimal TodayAttendancePercentage { get; set; }
    public int TodayUnmarked { get; set; }
    public int TodayLeave { get; set; }
    public decimal OverdueFees { get; set; }
    public int TodayPresent { get; set; }
    public int TodayAbsent { get; set; }
    public int TodayLate { get; set; }
    public int UnmarkedSections { get; set; }
    public int LowAttendanceStudents { get; set; }

    public decimal CurrentMonthExpectedFees { get; set; }
    public decimal CurrentMonthCollectedFees { get; set; }
    public decimal OutstandingFees { get; set; }
    public decimal CollectionRate { get; set; }
    public int FeeDefaulters { get; set; }
    public decimal CurrentMonthExpenses { get; set; }
    public decimal CurrentMonthPayroll { get; set; }
    public int PendingExpenseApprovals { get; set; }
    public int PendingPayrollRuns { get; set; }

    public int ActiveExams { get; set; }
    public int PublishedResultsThisMonth { get; set; }
    public int TeacherAssignments { get; set; }
    public int AssignedStudents { get; set; }

    public List<DashboardTrendPoint> FeeTrend { get; set; } = new();
    public List<DashboardTrendPoint> AttendanceTrend { get; set; } = new();
    public List<DashboardBarItem> ClassStrength { get; set; } = new();
    public List<DashboardBarItem> ExpenseBreakdown { get; set; } = new();
    public List<DashboardPaymentItem> RecentPayments { get; set; } = new();
    public List<DashboardAdmissionItem> RecentAdmissions { get; set; } = new();
    public List<DashboardExamItem> UpcomingExams { get; set; } = new();
    public List<DashboardAlertItem> Alerts { get; set; } = new();
    public List<DashboardAuditItem> RecentActivity { get; set; } = new();
}

public class DashboardTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal PrimaryValue { get; set; }
    public decimal SecondaryValue { get; set; }
}

public class DashboardBarItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Meta { get; set; }
}

public class DashboardPaymentItem
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class DashboardAdmissionItem
{
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class DashboardExamItem
{
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DashboardAlertItem
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Tone { get; set; } = "info";
    public string? Controller { get; set; }
    public string? Action { get; set; }
}

public class DashboardAuditItem
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
