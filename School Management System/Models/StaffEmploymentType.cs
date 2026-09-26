using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public enum StaffEmploymentType
{
    Permanent = 1,
    Contract = 2,
    Visiting = 3,
    [Display(Name = "Part Time")]
    PartTime = 4
}
