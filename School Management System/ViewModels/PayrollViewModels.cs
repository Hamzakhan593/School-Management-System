using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class SalaryStructureViewModel
{
    public int StaffId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int? SalaryStructureId { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Basic Salary")]
    public decimal BasicSalary { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "House Allowance")]
    public decimal HouseAllowance { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Medical Allowance")]
    public decimal MedicalAllowance { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Transport Allowance")]
    public decimal TransportAllowance { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Other Allowance")]
    public decimal OtherAllowance { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Fixed Monthly Deduction")]
    public decimal FixedDeduction { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Absent Deduction / Day")]
    public decimal AbsenceDeductionPerDay { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Half-Day Deduction / Day")]
    public decimal HalfDayDeductionPerDay { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Late Deduction / Occurrence")]
    public decimal LateDeductionPerOccurrence { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Leave Deduction / Day")]
    public decimal LeaveDeductionPerDay { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Effective From")]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;
    public IReadOnlyList<StaffAdvance> Advances { get; set; } = [];
}

public class StaffAdvanceFormViewModel
{
    [Required]
    public int StaffId { get; set; }

    public StaffAdvanceType Type { get; set; } = StaffAdvanceType.Advance;

    [Required, StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    [Display(Name = "Amount")]
    public decimal OriginalAmount { get; set; }

    [Range(typeof(decimal), "0.01", "999999999")]
    [Display(Name = "Monthly Installment")]
    public decimal MonthlyInstallment { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class PayrollIndexViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public IReadOnlyList<PayrollRun> Runs { get; set; } = [];
    public int StaffWithoutSalaryStructure { get; set; }
    public int PendingApprovedRuns { get; set; }
    public decimal PostedPayrollThisMonth { get; set; }
}

public class PayrollRunViewModel
{
    public PayrollRun Run { get; set; } = null!;
    public IReadOnlyList<PayrollItem> Items { get; set; } = [];
    public IReadOnlyList<PayrollDepartmentSummaryViewModel> DepartmentTotals { get; set; } = [];
    public decimal GrossTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal NetTotal { get; set; }
    public int InvalidItems { get; set; }
}

public class PayrollDepartmentSummaryViewModel
{
    public string Department { get; set; } = string.Empty;
    public int StaffCount { get; set; }
    public decimal GrossPay { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPay { get; set; }
}

public class PayrollAdjustmentViewModel
{
    [Required]
    public int RunId { get; set; }

    [Required]
    public int ItemId { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Manual Allowance")]
    public decimal ManualAllowance { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Manual Deduction")]
    public decimal ManualDeduction { get; set; }

    [StringLength(500)]
    [Display(Name = "Adjustment Note")]
    public string? ManualAdjustmentNote { get; set; }
}

public class PayrollPostViewModel
{
    [Required]
    public int RunId { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public PayrollPaymentMethod PaymentMethod { get; set; } = PayrollPaymentMethod.BankTransfer;

    [StringLength(150)]
    [Display(Name = "Payment / Batch Reference")]
    public string? PaymentReference { get; set; }
}

public class StaffPayrollHistoryViewModel
{
    public Staff Staff { get; set; } = null!;
    public SalaryStructure? SalaryStructure { get; set; }
    public IReadOnlyList<StaffAdvance> Advances { get; set; } = [];
    public IReadOnlyList<PayrollItem> PayrollItems { get; set; } = [];
}
