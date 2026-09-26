using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Staff
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(40)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30)]
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

    [DataType(DataType.Date)]
    public DateTime JoiningDate { get; set; }

    public StaffEmploymentType EmploymentType { get; set; } = StaffEmploymentType.Permanent;
    public StaffStatus Status { get; set; } = StaffStatus.Active;

    [StringLength(500)]
    public string? PhotoStorageKey { get; set; }

    // M12: optional link to the employee's active salary structure.
    public int? SalaryStructureId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ExitDate { get; set; }

    [StringLength(500)]
    public string? ExitReason { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public SalaryStructure? SalaryStructure { get; set; }
    public ICollection<StaffDocument> Documents { get; set; } = new List<StaffDocument>();
    public ICollection<StaffAttendance> Attendances { get; set; } = new List<StaffAttendance>();
    public ICollection<StaffAdvance> Advances { get; set; } = new List<StaffAdvance>();
}
