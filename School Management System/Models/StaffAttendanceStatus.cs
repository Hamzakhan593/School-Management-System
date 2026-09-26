using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public enum StaffAttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Leave = 4,
    [Display(Name = "Half Day")]
    HalfDay = 5,
    Holiday = 6
}
