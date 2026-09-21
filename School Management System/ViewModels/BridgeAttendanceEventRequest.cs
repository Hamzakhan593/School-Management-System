using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class BridgeAttendanceEventRequest
{
    [Required, StringLength(80)]
    public string DeviceCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string EventId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string DeviceUserReference { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
    public AttendanceDirection Direction { get; set; } = AttendanceDirection.In;

    [StringLength(250)]
    public string? RawReference { get; set; }
}
