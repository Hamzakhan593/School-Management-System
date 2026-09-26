using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class StaffIndexViewModel
{
    public IReadOnlyList<Staff> StaffMembers { get; set; } = [];
    public string? Query { get; set; }
    public StaffStatus? Status { get; set; }
    public StaffEmploymentType? EmploymentType { get; set; }
    public string? Department { get; set; }
    public IReadOnlyList<string> Departments { get; set; } = [];
    public int TotalStaff { get; set; }
    public int ActiveStaff { get; set; }
    public int TeachingStaff { get; set; }
}

public class StaffFormViewModel
{
    public int Id { get; set; }
    public string? EmployeeId { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Required, StringLength(120)]
    public string Designation { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Department { get; set; }

    [StringLength(500)]
    public string? Qualification { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Joining Date")]
    public DateTime JoiningDate { get; set; } = DateTime.Today;

    [Display(Name = "Employment Type")]
    public StaffEmploymentType EmploymentType { get; set; } = StaffEmploymentType.Permanent;

    public StaffStatus Status { get; set; } = StaffStatus.Active;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public string? RowVersion { get; set; }
}

public class StaffDetailsViewModel
{
    public Staff Staff { get; set; } = null!;
    public ApplicationUser? LinkedUser { get; set; }
    public IReadOnlyList<string> LinkedUserRoles { get; set; } = [];
    public IReadOnlyList<ApplicationUser> AvailableUserAccounts { get; set; } = [];
    public IReadOnlyList<TeacherAssignment> TeacherAssignments { get; set; } = [];
    public IReadOnlyList<Section> ClassTeacherSections { get; set; } = [];
    public IReadOnlyList<StaffDocument> Documents { get; set; } = [];
}

public class StaffDocumentUploadViewModel
{
    [Required]
    public int StaffId { get; set; }

    [Required]
    [Display(Name = "Document Type")]
    public StaffDocumentType DocumentType { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}

public class StaffExitViewModel
{
    [Required]
    public int StaffId { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Exit Date")]
    public DateTime ExitDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Exit Status")]
    public StaffStatus Status { get; set; } = StaffStatus.Resigned;

    [Required, StringLength(500)]
    [Display(Name = "Reason / Remarks")]
    public string ExitReason { get; set; } = string.Empty;
}
