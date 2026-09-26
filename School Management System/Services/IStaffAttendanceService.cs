using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public record StaffAttendanceSaveResult(bool Success, string Message, int CreatedCount = 0, int UpdatedCount = 0);

public record StaffAttendanceIntegrationResult(
    bool Success,
    AttendanceEventProcessingStatus Status,
    string Message,
    long? EventId = null,
    int? AttendanceId = null,
    int? StaffId = null);

public interface IStaffAttendanceService
{
    Task<StaffAttendanceMarkingViewModel> BuildDailyAsync(int schoolId, DateTime date, CancellationToken cancellationToken = default);
    Task<StaffAttendanceSaveResult> SaveDailyAsync(int schoolId, string userId, StaffAttendanceMarkingViewModel model, CancellationToken cancellationToken = default);
    Task<StaffAttendanceMonthlyReportViewModel> BuildMonthlyReportAsync(int schoolId, DateTime month, CancellationToken cancellationToken = default);
    Task<StaffAttendanceIntegrationResult> ProcessBiometricEventAsync(BiometricDevice device, BridgeAttendanceEventRequest request, CancellationToken cancellationToken = default);
    Task<StaffAttendanceIntegrationResult> MarkCameraVerifiedAsync(int schoolId, string userId, int staffId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default);
}
