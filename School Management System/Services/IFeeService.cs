using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IFeeService
{
    Task<FeeGenerationPreviewViewModel> PreviewGenerationAsync(int schoolId, ChallanGenerationViewModel request, CancellationToken cancellationToken = default);
    Task<FeeChallanBatch> GenerateAsync(int schoolId, string? userId, ChallanGenerationViewModel request, CancellationToken cancellationToken = default);
    Task<FeeChallan> RegenerateChallanAsync(int schoolId, int challanId, string? userId, CancellationToken cancellationToken = default);
    Task CancelChallanAsync(int schoolId, int challanId, string reason, string? userId, CancellationToken cancellationToken = default);
    Task<FeePayment> ReceivePaymentAsync(int schoolId, string? userId, PaymentEntryViewModel request, CancellationToken cancellationToken = default);
    Task ReversePaymentAsync(int schoolId, int paymentId, string reason, string? userId, CancellationToken cancellationToken = default);
    Task RefreshStatusesAndLateFeesAsync(int schoolId, CancellationToken cancellationToken = default);
    Task<decimal> GetStudentOutstandingAsync(int schoolId, int studentId, CancellationToken cancellationToken = default);
}
