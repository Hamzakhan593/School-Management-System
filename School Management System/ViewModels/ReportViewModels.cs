namespace School_Management_System.ViewModels;

public class StudentRegisterReportViewModel
{
    public string SchoolName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public int? SchoolClassId { get; set; }
    public string? Search { get; set; }
    public List<ReportOption> Classes { get; set; } = new();
    public List<StudentRegisterRow> Rows { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class StudentRegisterRow
{
    public int StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GuardianName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AuditActivityReportViewModel
{
    public string? Search { get; set; }
    public string? ActionName { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public List<AuditActivityRow> Rows { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class AuditActivityRow
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class ReportOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
