using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public enum StaffStatus
{
    Active = 1,
    [Display(Name = "On Leave")]
    OnLeave = 2,
    Inactive = 3,
    Resigned = 4,
    Terminated = 5,
    Retired = 6
}
