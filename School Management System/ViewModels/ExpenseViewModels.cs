using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class ExpenseCategoryFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 9999)]
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExpenseCategoriesViewModel
{
    public List<ExpenseCategory> Categories { get; set; } = [];
    public ExpenseCategoryFormViewModel Form { get; set; } = new();
}

public class ExpenseFormViewModel
{
    public int? Id { get; set; }

    [Required]
    public int ExpenseCategoryId { get; set; }

    [DataType(DataType.Date)]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal Amount { get; set; }

    [Required, StringLength(180)]
    public string PaidTo { get; set; } = string.Empty;

    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;

    [StringLength(150)]
    public string? ReferenceNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public IFormFile? Attachment { get; set; }
    public string? ExistingAttachmentName { get; set; }
    public string? RowVersion { get; set; }
    public List<ExpenseCategory> Categories { get; set; } = [];
}

public class ExpensesIndexViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal MonthApprovedTotal { get; set; }
    public decimal MonthPendingTotal { get; set; }
    public int PendingApprovalCount { get; set; }
    public List<Expense> RecentExpenses { get; set; } = [];
}

public class ExpenseReportViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public List<ExpenseCategory> Categories { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
    public List<ExpenseCategoryTotalViewModel> CategoryTotals { get; set; } = [];
    public decimal ApprovedTotal { get; set; }
    public decimal PendingTotal { get; set; }
    public decimal RejectedTotal { get; set; }
}

public class ExpenseCategoryTotalViewModel
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class OtherIncomeFormViewModel
{
    [DataType(DataType.Date)]
    public DateTime IncomeDate { get; set; } = DateTime.Today;

    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal Amount { get; set; }

    [Required, StringLength(180)]
    public string Source { get; set; } = string.Empty;

    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;

    [StringLength(150)]
    public string? ReferenceNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class OtherIncomeIndexViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal MonthTotal { get; set; }
    public OtherIncomeFormViewModel Form { get; set; } = new();
    public List<OtherIncome> Items { get; set; } = [];
}

public class FinancialSummaryViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal FeeIncome { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal TotalIncome => FeeIncome + OtherIncome;
    public decimal PayrollCost { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal TotalOutflow => PayrollCost + OperatingExpenses;
    public decimal NetBalance => TotalIncome - TotalOutflow;
    public List<FinancialLedgerRowViewModel> LedgerRows { get; set; } = [];
    public List<FinancialYearMonthViewModel> YearRows { get; set; } = [];
}

public class FinancialLedgerRowViewModel
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
}

public class FinancialYearMonthViewModel
{
    public int Month { get; set; }
    public decimal FeeIncome { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal PayrollCost { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal Net => FeeIncome + OtherIncome - PayrollCost - OperatingExpenses;
}
