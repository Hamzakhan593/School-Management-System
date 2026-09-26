using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class SalaryStructure
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    public decimal BasicSalary { get; set; }
    public decimal HouseAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal FixedDeduction { get; set; }

    public decimal AbsenceDeductionPerDay { get; set; }
    public decimal HalfDayDeductionPerDay { get; set; }
    public decimal LateDeductionPerOccurrence { get; set; }
    public decimal LeaveDeductionPerDay { get; set; }

    [DataType(DataType.Date)]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();
}
