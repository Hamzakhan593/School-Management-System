using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class StaffAttendanceMarkingViewModel
{
    [DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; } = DateTime.Today;
    public List<StaffAttendanceRowViewModel> StaffMembers { get; set; } = [];
}

public class StaffAttendanceRowViewModel
{
    public int StaffId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? Department { get; set; }
    public StaffAttendanceStatus Status { get; set; } = StaffAttendanceStatus.Present;
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;
    public DateTime? CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }
    public bool AlreadySaved { get; set; }
}

public class StaffAttendanceMonthlyReportViewModel
{
    public DateTime Month { get; set; }
    public IReadOnlyList<StaffAttendanceMonthlyRowViewModel> Rows { get; set; } = [];
}

public class StaffAttendanceMonthlyRowViewModel
{
    public int StaffId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int LeaveDays { get; set; }
    public int HalfDays { get; set; }
    public int HolidayDays { get; set; }
    public int TotalMarkedDays { get; set; }
}

public class StaffAttendanceIntegrationsViewModel
{
    public IReadOnlyList<BiometricDevice> Devices { get; set; } = [];
    public IReadOnlyList<StaffBiometricEnrollment> Enrollments { get; set; } = [];
    public IReadOnlyList<Staff> StaffMembers { get; set; } = [];
    public StaffBiometricEnrollmentFormViewModel Form { get; set; } = new();
}

public class StaffBiometricEnrollmentFormViewModel
{
    [Required]
    [Display(Name = "Attendance Device")]
    public int BiometricDeviceId { get; set; }

    [Required]
    [Display(Name = "Staff Member")]
    public int StaffId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Device User Reference")]
    public string DeviceUserReference { get; set; } = string.Empty;

    public BiometricModality Modality { get; set; } = BiometricModality.Fingerprint;

    [StringLength(200)]
    [Display(Name = "Template Reference")]
    public string? TemplateReference { get; set; }
}

public class StaffCameraAttendanceViewModel
{
    [Required]
    [Display(Name = "Staff Member")]
    public int StaffId { get; set; }
    public IReadOnlyList<Staff> StaffMembers { get; set; } = [];
}
