using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public record PayrollOperationResult(bool Success, string Message, int? RunId = null);

public interface IStaffPayrollService
{
    Task<PayrollIndexViewModel> BuildIndexAsync(int schoolId, int year, int month, CancellationToken cancellationToken = default);
    Task<SalaryStructureViewModel?> BuildSalaryStructureAsync(int schoolId, int staffId, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> SaveSalaryStructureAsync(int schoolId, SalaryStructureViewModel model, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> AddAdvanceAsync(int schoolId, StaffAdvanceFormViewModel model, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> CancelAdvanceAsync(int schoolId, int advanceId, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> GenerateRunAsync(int schoolId, string userId, int year, int month, CancellationToken cancellationToken = default);
    Task<PayrollRunViewModel?> BuildRunAsync(int schoolId, int runId, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> UpdateAdjustmentAsync(int schoolId, PayrollAdjustmentViewModel model, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> RecalculateAsync(int schoolId, int runId, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> ValidateAsync(int schoolId, string userId, int runId, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> ApproveAsync(int schoolId, string userId, int runId, CancellationToken cancellationToken = default);
    Task<PayrollOperationResult> PostAsync(int schoolId, string userId, PayrollPostViewModel model, CancellationToken cancellationToken = default);
    Task<StaffPayrollHistoryViewModel?> BuildStaffHistoryAsync(int schoolId, int staffId, CancellationToken cancellationToken = default);
}
