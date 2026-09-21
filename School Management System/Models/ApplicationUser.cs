using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace School_Management_System.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // These links are intentionally nullable until School (M02) and Staff (M11) exist.
    public int? SchoolId { get; set; }
    public int? StaffId { get; set; }

    public bool ForcePasswordChange { get; set; }
}
