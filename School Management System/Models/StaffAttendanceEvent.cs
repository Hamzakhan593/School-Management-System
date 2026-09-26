using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StaffAttendanceEvent
{
    public long Id { get; set; }
    public int SchoolId { get; set; }
    public int? StaffId { get; set; }
    public int? StaffAttendanceId { get; set; }
    public int? BiometricDeviceId { get; set; }

    [StringLength(120)]
    public string? ExternalEventId { get; set; }

    [StringLength(100)]
    public string? DeviceUserReference { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    [DataType(DataType.Date)]
    public DateTime LocalDate { get; set; }

    public AttendanceSource Source { get; set; }
    public AttendanceDirection Direction { get; set; } = AttendanceDirection.Unknown;
    public AttendanceEventProcessingStatus ProcessingStatus { get; set; } = AttendanceEventProcessingStatus.Pending;

    [StringLength(500)]
    public string? ReviewReason { get; set; }

    [StringLength(250)]
    public string? RawReference { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }

    public School School { get; set; } = null!;
    public Staff? Staff { get; set; }
    public StaffAttendance? StaffAttendance { get; set; }
    public BiometricDevice? BiometricDevice { get; set; }
}
