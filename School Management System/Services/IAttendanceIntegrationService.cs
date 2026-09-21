using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public record AttendanceIntegrationResult(
    bool Success,
    AttendanceEventProcessingStatus Status,
    string Message,
    long? EventId = null,
    int? AttendanceId = null,
    int? StudentId = null);

public interface IAttendanceIntegrationService
{
    Task<AttendanceIntegrationResult> ProcessBiometricEventAsync(
        BiometricDevice device,
        BridgeAttendanceEventRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceIntegrationResult> ProcessCameraAttendanceAsync(
        int schoolId,
        string currentUserId,
        int studentId,
        DateTimeOffset occurredAt,
        decimal? confidence,
        int? faceCount,
        string? reviewReason,
        CancellationToken cancellationToken = default);

    Task<long> RecordCameraReviewEventAsync(
        int schoolId,
        string currentUserId,
        DateTimeOffset occurredAt,
        int? candidateStudentId,
        decimal? confidence,
        int? faceCount,
        string reason,
        CancellationToken cancellationToken = default);

    DateTimeOffset GetSchoolLocalNow();
}
