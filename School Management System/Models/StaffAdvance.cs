using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StaffAdvance
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StaffId { get; set; }

    public StaffAdvanceType Type { get; set; } = StaffAdvanceType.Advance;

    [Required, StringLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal OriginalAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal MonthlyInstallment { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    public StaffAdvanceStatus Status { get; set; } = StaffAdvanceStatus.Active;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
}
