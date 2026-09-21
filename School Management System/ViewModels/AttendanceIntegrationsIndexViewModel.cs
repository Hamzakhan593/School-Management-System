using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AttendanceIntegrationsIndexViewModel
{
    public IReadOnlyList<BiometricDevice> Devices { get; set; } = [];
    public IReadOnlyList<BiometricEnrollment> Enrollments { get; set; } = [];
    public IReadOnlyList<CameraFaceEnrollment> CameraEnrollments { get; set; } = [];
    public IReadOnlyList<AttendanceEvent> RecentEvents { get; set; } = [];
    public string? OneTimeDeviceKey { get; set; }
    public string? OneTimeDeviceCode { get; set; }
}
