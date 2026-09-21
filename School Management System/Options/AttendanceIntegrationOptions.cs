namespace School_Management_System.Options;

public class AttendanceIntegrationOptions
{
    public int DuplicateWindowSeconds { get; set; } = 60;
    public decimal CameraAutoAcceptConfidence { get; set; } = 0.85m;
    public int MaxCameraImageBytes { get; set; } = 2_000_000;
    public string SchoolTimeZoneId { get; set; } = "Asia/Karachi";
}
