using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StaffAttendance
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StaffId { get; set; }

    [DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; }

    public StaffAttendanceStatus Status { get; set; } = StaffAttendanceStatus.Present;
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;

    public DateTime? CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }

    [StringLength(450)]
    public string? MarkedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
}
