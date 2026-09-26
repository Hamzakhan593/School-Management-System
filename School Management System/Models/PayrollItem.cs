using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class PayrollItem
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int PayrollRunId { get; set; }
    public int StaffId { get; set; }
    public int? SalaryStructureId { get; set; }

    [Required, StringLength(40)]
    public string EmployeeIdSnapshot { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string StaffNameSnapshot { get; set; } = string.Empty;

    [StringLength(120)]
    public string? DepartmentSnapshot { get; set; }

    public decimal BasicSalary { get; set; }
    public decimal FixedAllowances { get; set; }
    public decimal ManualAllowance { get; set; }
    public decimal GrossPay { get; set; }

    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int LeaveDays { get; set; }
    public int HalfDays { get; set; }

    public decimal AttendanceDeduction { get; set; }
    public decimal FixedDeduction { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public decimal ManualDeduction { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    [StringLength(500)]
    public string? ManualAdjustmentNote { get; set; }

    [StringLength(500)]
    public string? ValidationMessage { get; set; }

    public bool IsValid { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public PayrollRun PayrollRun { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
    public SalaryStructure? SalaryStructure { get; set; }
    public ICollection<PayrollAdvanceDeduction> AdvanceDeductions { get; set; } = new List<PayrollAdvanceDeduction>();
}
