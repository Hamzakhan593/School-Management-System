using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IFinanceService
{
    Task<ExpensesIndexViewModel> BuildExpensesIndexAsync(int schoolId, int year, int month, CancellationToken cancellationToken = default);
    Task<ExpenseReportViewModel> BuildExpenseReportAsync(int schoolId, int year, int month, int? categoryId, CancellationToken cancellationToken = default);
    Task<FinancialSummaryViewModel> BuildFinancialSummaryAsync(int schoolId, int year, int month, CancellationToken cancellationToken = default);
}
