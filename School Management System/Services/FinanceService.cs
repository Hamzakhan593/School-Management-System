using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class FinanceService : IFinanceService
{
    private readonly ApplicationDbContext _db;

    public FinanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ExpensesIndexViewModel> BuildExpensesIndexAsync(int schoolId, int year, int month, CancellationToken cancellationToken = default)
    {
        var (start, end) = MonthRange(year, month);
        var monthRows = await _db.Expenses.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && !x.IsCancelled && x.ExpenseDate >= start && x.ExpenseDate < end)
            .ToListAsync(cancellationToken);

        var countPending = await _db.Expenses.AsNoTracking()
            .CountAsync(x => x.SchoolId == schoolId && !x.IsCancelled && x.ApprovalStatus == ExpenseApprovalStatus.Pending, cancellationToken);

        var recent = await _db.Expenses.AsNoTracking().Include(x => x.ExpenseCategory)
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.ExpenseDate).ThenByDescending(x => x.Id)
            .Take(15)
            .ToListAsync(cancellationToken);

        return new ExpensesIndexViewModel
        {
            Year = year,
            Month = month,
            MonthApprovedTotal = monthRows.Where(IsCountableExpense).Sum(x => x.Amount),
            MonthPendingTotal = monthRows.Where(x => x.ApprovalStatus == ExpenseApprovalStatus.Pending).Sum(x => x.Amount),
            PendingApprovalCount = countPending,
            RecentExpenses = recent
        };
    }

    public async Task<ExpenseReportViewModel> BuildExpenseReportAsync(int schoolId, int year, int month, int? categoryId, CancellationToken cancellationToken = default)
    {
        var (start, end) = MonthRange(year, month);
        var query = _db.Expenses.AsNoTracking().Include(x => x.ExpenseCategory)
            .Where(x => x.SchoolId == schoolId && !x.IsCancelled && x.ExpenseDate >= start && x.ExpenseDate < end);

        if (categoryId.HasValue)
            query = query.Where(x => x.ExpenseCategoryId == categoryId.Value);

        var rows = await query.OrderByDescending(x => x.ExpenseDate).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);
        var approved = rows.Where(IsCountableExpense).ToList();

        return new ExpenseReportViewModel
        {
            Year = year,
            Month = month,
            ExpenseCategoryId = categoryId,
            Categories = await _db.ExpenseCategories.AsNoTracking().Where(x => x.SchoolId == schoolId).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken),
            Expenses = rows,
            CategoryTotals = approved.GroupBy(x => x.ExpenseCategory.Name).Select(g => new ExpenseCategoryTotalViewModel { Category = g.Key, Amount = g.Sum(x => x.Amount) }).OrderByDescending(x => x.Amount).ToList(),
            ApprovedTotal = approved.Sum(x => x.Amount),
            PendingTotal = rows.Where(x => x.ApprovalStatus == ExpenseApprovalStatus.Pending).Sum(x => x.Amount),
            RejectedTotal = rows.Where(x => x.ApprovalStatus == ExpenseApprovalStatus.Rejected).Sum(x => x.Amount)
        };
    }

    public async Task<FinancialSummaryViewModel> BuildFinancialSummaryAsync(int schoolId, int year, int month, CancellationToken cancellationToken = default)
    {
        var model = await BuildMonthSummaryAsync(schoolId, year, month, includeLedger: true, cancellationToken);
        for (var m = 1; m <= 12; m++)
        {
            var row = await BuildMonthSummaryAsync(schoolId, year, m, includeLedger: false, cancellationToken);
            model.YearRows.Add(new FinancialYearMonthViewModel
            {
                Month = m,
                FeeIncome = row.FeeIncome,
                OtherIncome = row.OtherIncome,
                PayrollCost = row.PayrollCost,
                OperatingExpenses = row.OperatingExpenses
            });
        }
        return model;
    }

    private async Task<FinancialSummaryViewModel> BuildMonthSummaryAsync(int schoolId, int year, int month, bool includeLedger, CancellationToken cancellationToken)
    {
        var (start, end) = MonthRange(year, month);
        var (utcStart, utcEnd) = ToUtcRange(start, end);

        var feePayments = await _db.FeePayments.AsNoTracking().Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId && !x.IsReversed && x.PaymentDateUtc >= utcStart && x.PaymentDateUtc < utcEnd)
            .ToListAsync(cancellationToken);

        var otherIncome = await _db.OtherIncomes.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && !x.IsCancelled && x.IncomeDate >= start && x.IncomeDate < end)
            .ToListAsync(cancellationToken);

        var expenses = await _db.Expenses.AsNoTracking().Include(x => x.ExpenseCategory)
            .Where(x => x.SchoolId == schoolId && !x.IsCancelled && x.ExpenseDate >= start && x.ExpenseDate < end
                        && (x.ApprovalStatus == ExpenseApprovalStatus.NotRequired || x.ApprovalStatus == ExpenseApprovalStatus.Approved))
            .ToListAsync(cancellationToken);

        var payrollRuns = await _db.PayrollRuns.AsNoTracking().Include(x => x.Items)
            .Where(x => x.SchoolId == schoolId && x.PeriodYear == year && x.PeriodMonth == month && x.Status == PayrollRunStatus.Posted)
            .ToListAsync(cancellationToken);

        var model = new FinancialSummaryViewModel
        {
            Year = year,
            Month = month,
            FeeIncome = feePayments.Sum(x => x.Amount),
            OtherIncome = otherIncome.Sum(x => x.Amount),
            OperatingExpenses = expenses.Sum(x => x.Amount),
            PayrollCost = payrollRuns.SelectMany(x => x.Items).Sum(x => x.NetPay)
        };

        if (!includeLedger)
            return model;

        foreach (var p in feePayments)
        {
            model.LedgerRows.Add(new FinancialLedgerRowViewModel
            {
                Date = p.PaymentDateUtc.ToLocalTime(),
                Type = "Fee Income",
                Reference = p.ReceiptNumber,
                Description = p.Student?.FullName ?? "Student fee payment",
                Inflow = p.Amount
            });
        }

        foreach (var i in otherIncome)
        {
            model.LedgerRows.Add(new FinancialLedgerRowViewModel
            {
                Date = i.IncomeDate,
                Type = "Other Income",
                Reference = i.ReferenceNumber ?? $"INC-{i.Id}",
                Description = i.Source,
                Inflow = i.Amount
            });
        }

        foreach (var e in expenses)
        {
            model.LedgerRows.Add(new FinancialLedgerRowViewModel
            {
                Date = e.ExpenseDate,
                Type = "Expense",
                Reference = e.ReferenceNumber ?? $"EXP-{e.Id}",
                Description = $"{e.ExpenseCategory.Name} - {e.PaidTo}",
                Outflow = e.Amount
            });
        }

        foreach (var r in payrollRuns)
        {
            model.LedgerRows.Add(new FinancialLedgerRowViewModel
            {
                Date = r.PostedAtUtc?.ToLocalTime() ?? new DateTime(year, month, 1),
                Type = "Payroll",
                Reference = $"PAYROLL-{r.Id}",
                Description = $"Posted payroll {year}-{month:00} ({r.Items.Count} staff)",
                Outflow = r.Items.Sum(x => x.NetPay)
            });
        }

        model.LedgerRows = model.LedgerRows.OrderByDescending(x => x.Date).ThenBy(x => x.Type).ToList();
        return model;
    }

    private static bool IsCountableExpense(Expense x)
        => x.ApprovalStatus is ExpenseApprovalStatus.NotRequired or ExpenseApprovalStatus.Approved;

    private static (DateTime Start, DateTime End) MonthRange(int year, int month)
    {
        if (year < 2000 || year > 2200) year = DateTime.Today.Year;
        if (month < 1 || month > 12) month = DateTime.Today.Month;
        var start = new DateTime(year, month, 1);
        return (start, start.AddMonths(1));
    }

    private static (DateTime UtcStart, DateTime UtcEnd) ToUtcRange(DateTime localStart, DateTime localEnd)
    {
        foreach (var id in new[] { "Pakistan Standard Time", "Asia/Karachi" })
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(id);
                return (
                    TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified), zone),
                    TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified), zone));
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        // Pakistan Standard Time is UTC+5. This fallback keeps month boundaries correct on minimal hosts.
        return (DateTime.SpecifyKind(localStart.AddHours(-5), DateTimeKind.Utc), DateTime.SpecifyKind(localEnd.AddHours(-5), DateTimeKind.Utc));
    }
}
