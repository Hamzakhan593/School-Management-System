using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Options;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class AttendanceIntegrationService : IAttendanceIntegrationService
{
    private readonly ApplicationDbContext _db;
    private readonly AttendanceIntegrationOptions _options;
    private readonly ILogger<AttendanceIntegrationService> _logger;

    public AttendanceIntegrationService(
        ApplicationDbContext db,
        IOptions<AttendanceIntegrationOptions> options,
        ILogger<AttendanceIntegrationService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public DateTimeOffset GetSchoolLocalNow()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(_options.SchoolTimeZoneId);
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Configured school time zone {TimeZoneId} was not found. Falling back to server local time.", _options.SchoolTimeZoneId);
            return DateTimeOffset.Now;
        }
        catch (InvalidTimeZoneException)
        {
            _logger.LogWarning("Configured school time zone {TimeZoneId} is invalid. Falling back to server local time.", _options.SchoolTimeZoneId);
            return DateTimeOffset.Now;
        }
    }

    public async Task<AttendanceIntegrationResult> ProcessBiometricEventAsync(
        BiometricDevice device,
        BridgeAttendanceEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var externalId = request.EventId.Trim();
        var userReference = request.DeviceUserReference.Trim();
        var occurredAt = request.OccurredAt == default ? DateTimeOffset.UtcNow : request.OccurredAt;
        var localDate = occurredAt.Date;

        var exactExisting = await _db.AttendanceEvents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BiometricDeviceId == device.Id && x.ExternalEventId == externalId, cancellationToken);

        if (exactExisting is not null)
        {
            return new AttendanceIntegrationResult(
                true,
                exactExisting.ProcessingStatus,
                "This event was already received. Existing result returned without creating a duplicate.",
                exactExisting.Id,
                exactExisting.StudentAttendanceId,
                exactExisting.StudentId);
        }

        var biometricEnrollment = await _db.BiometricEnrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == device.SchoolId
                && x.BiometricDeviceId == device.Id
                && x.DeviceUserReference == userReference
                && x.IsActive,
                cancellationToken);

        if (biometricEnrollment is null)
        {
            var unmatched = new AttendanceEvent
            {
                SchoolId = device.SchoolId,
                BiometricDeviceId = device.Id,
                ExternalEventId = externalId,
                DeviceUserReference = userReference,
                OccurredAtUtc = occurredAt.UtcDateTime,
                LocalDate = localDate,
                Source = AttendanceSource.Biometric,
                Direction = request.Direction,
                ProcessingStatus = AttendanceEventProcessingStatus.Unmatched,
                ReviewReason = "No active biometric enrollment matches this device user reference.",
                RawReference = Clean(request.RawReference, 250),
                ProcessedAtUtc = DateTime.UtcNow
            };

            _db.AttendanceEvents.Add(unmatched);
            device.LastSyncAtUtc = DateTime.UtcNow;
            device.LastStatusMessage = $"Unmatched user reference: {userReference}";
            await _db.SaveChangesAsync(cancellationToken);

            return new AttendanceIntegrationResult(false, unmatched.ProcessingStatus, unmatched.ReviewReason!, unmatched.Id);
        }

        var duplicateWindow = Math.Max(0, _options.DuplicateWindowSeconds);
        if (duplicateWindow > 0)
        {
            var start = occurredAt.UtcDateTime.AddSeconds(-duplicateWindow);
            var end = occurredAt.UtcDateTime.AddSeconds(duplicateWindow);
            var repeatedScan = await _db.AttendanceEvents.AsNoTracking()
                .Where(x => x.SchoolId == device.SchoolId
                    && x.BiometricDeviceId == device.Id
                    && x.StudentId == biometricEnrollment.StudentId
                    && x.Source == AttendanceSource.Biometric
                    && x.Direction == request.Direction
                    && x.OccurredAtUtc >= start
                    && x.OccurredAtUtc <= end
                    && x.ProcessingStatus != AttendanceEventProcessingStatus.Rejected
                    && x.ProcessingStatus != AttendanceEventProcessingStatus.Duplicate)
                .OrderByDescending(x => x.OccurredAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (repeatedScan is not null)
            {
                var duplicate = new AttendanceEvent
                {
                    SchoolId = device.SchoolId,
                    StudentId = biometricEnrollment.StudentId,
                    BiometricDeviceId = device.Id,
                    ExternalEventId = externalId,
                    DeviceUserReference = userReference,
                    OccurredAtUtc = occurredAt.UtcDateTime,
                    LocalDate = localDate,
                    Source = AttendanceSource.Biometric,
                    Direction = request.Direction,
                    ProcessingStatus = AttendanceEventProcessingStatus.Duplicate,
                    ReviewReason = $"Repeated scan inside the {duplicateWindow}-second de-duplication window.",
                    RawReference = Clean(request.RawReference, 250),
                    ProcessedAtUtc = DateTime.UtcNow
                };

                _db.AttendanceEvents.Add(duplicate);
                device.LastSyncAtUtc = DateTime.UtcNow;
                device.LastStatusMessage = "Connected - duplicate scan ignored";
                await _db.SaveChangesAsync(cancellationToken);

                return new AttendanceIntegrationResult(true, duplicate.ProcessingStatus, duplicate.ReviewReason!, duplicate.Id, null, biometricEnrollment.StudentId);
            }
        }

        var attendanceEvent = new AttendanceEvent
        {
            SchoolId = device.SchoolId,
            StudentId = biometricEnrollment.StudentId,
            BiometricDeviceId = device.Id,
            ExternalEventId = externalId,
            DeviceUserReference = userReference,
            OccurredAtUtc = occurredAt.UtcDateTime,
            LocalDate = localDate,
            Source = AttendanceSource.Biometric,
            Direction = request.Direction,
            RawReference = Clean(request.RawReference, 250)
        };

        var result = await ApplyPresenceAsync(
            attendanceEvent,
            device.SchoolId,
            biometricEnrollment.StudentId,
            localDate,
            AttendanceSource.Biometric,
            request.Direction,
            null,
            cancellationToken);

        device.LastSyncAtUtc = DateTime.UtcNow;
        device.LastStatusMessage = result.Status is AttendanceEventProcessingStatus.AttendanceApplied or AttendanceEventProcessingStatus.Recorded
            ? "Connected - last event processed"
            : $"Connected - {result.Status}";
        await _db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<AttendanceIntegrationResult> ProcessCameraAttendanceAsync(
        int schoolId,
        string currentUserId,
        int studentId,
        DateTimeOffset occurredAt,
        decimal? confidence,
        int? faceCount,
        string? reviewReason,
        CancellationToken cancellationToken = default)
    {
        var attendanceEvent = new AttendanceEvent
        {
            SchoolId = schoolId,
            StudentId = studentId,
            OccurredAtUtc = occurredAt.UtcDateTime,
            LocalDate = occurredAt.Date,
            Source = AttendanceSource.Camera,
            Direction = AttendanceDirection.In,
            ConfidenceScore = NormalizeConfidence(confidence),
            DetectedFaceCount = faceCount,
            ReviewReason = Clean(reviewReason, 500),
            ProcessedByUserId = currentUserId
        };

        return await ApplyPresenceAsync(
            attendanceEvent,
            schoolId,
            studentId,
            occurredAt.Date,
            AttendanceSource.Camera,
            AttendanceDirection.In,
            currentUserId,
            cancellationToken);
    }

    public async Task<long> RecordCameraReviewEventAsync(
        int schoolId,
        string currentUserId,
        DateTimeOffset occurredAt,
        int? candidateStudentId,
        decimal? confidence,
        int? faceCount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var attendanceEvent = new AttendanceEvent
        {
            SchoolId = schoolId,
            StudentId = candidateStudentId,
            OccurredAtUtc = occurredAt.UtcDateTime,
            LocalDate = occurredAt.Date,
            Source = AttendanceSource.Camera,
            Direction = AttendanceDirection.In,
            ProcessingStatus = AttendanceEventProcessingStatus.NeedsReview,
            ConfidenceScore = NormalizeConfidence(confidence),
            DetectedFaceCount = faceCount,
            ReviewReason = Clean(reason, 500),
            ProcessedByUserId = currentUserId,
            ProcessedAtUtc = DateTime.UtcNow
        };

        _db.AttendanceEvents.Add(attendanceEvent);
        await _db.SaveChangesAsync(cancellationToken);
        return attendanceEvent.Id;
    }

    private async Task<AttendanceIntegrationResult> ApplyPresenceAsync(
        AttendanceEvent attendanceEvent,
        int schoolId,
        int studentId,
        DateTime localDate,
        AttendanceSource source,
        AttendanceDirection direction,
        string? markedByUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var student = await _db.Students.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == studentId && x.SchoolId == schoolId, cancellationToken);

        if (student is null || student.Status != StudentStatus.Active)
        {
            attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.NeedsReview;
            attendanceEvent.ReviewReason = AppendReason(attendanceEvent.ReviewReason, "Student is missing or not active.");
            attendanceEvent.ProcessedAtUtc = DateTime.UtcNow;
            _db.AttendanceEvents.Add(attendanceEvent);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(false, attendanceEvent.ProcessingStatus, attendanceEvent.ReviewReason!, attendanceEvent.Id, null, studentId);
        }

        if (direction == AttendanceDirection.Out)
        {
            attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.Recorded;
            attendanceEvent.ReviewReason = AppendReason(attendanceEvent.ReviewReason, "OUT event recorded; daily attendance was not changed.");
            attendanceEvent.ProcessedAtUtc = DateTime.UtcNow;
            _db.AttendanceEvents.Add(attendanceEvent);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, attendanceEvent.ProcessingStatus, "OUT event recorded.", attendanceEvent.Id, null, studentId);
        }

        var enrollment = await _db.StudentEnrollments
            .Include(x => x.AcademicSession)
            .Where(x => x.SchoolId == schoolId
                && x.StudentId == studentId
                && x.SchoolClassId != null
                && x.EffectiveFrom <= localDate
                && (x.EffectiveTo == null || x.EffectiveTo >= localDate)
                && x.AcademicSession.StartDate <= localDate
                && x.AcademicSession.EndDate >= localDate)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment is null)
        {
            attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.NeedsReview;
            attendanceEvent.ReviewReason = AppendReason(attendanceEvent.ReviewReason, "No normalized class enrollment was found for this student/date.");
            attendanceEvent.ProcessedAtUtc = DateTime.UtcNow;
            _db.AttendanceEvents.Add(attendanceEvent);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(false, attendanceEvent.ProcessingStatus, attendanceEvent.ReviewReason!, attendanceEvent.Id, null, studentId);
        }

        attendanceEvent.StudentEnrollmentId = enrollment.Id;

        var existingAttendance = await _db.StudentAttendances
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId
                && x.AcademicSessionId == enrollment.AcademicSessionId
                && x.StudentId == studentId
                && x.AttendanceDate == localDate,
                cancellationToken);

        if (existingAttendance is not null)
        {
            attendanceEvent.StudentAttendanceId = existingAttendance.Id;
            attendanceEvent.ProcessedAtUtc = DateTime.UtcNow;

            if (existingAttendance.Status is StudentAttendanceStatus.Present or StudentAttendanceStatus.Late)
            {
                attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.Recorded;
                attendanceEvent.ReviewReason = AppendReason(attendanceEvent.ReviewReason, $"Attendance already exists as {existingAttendance.Status}; it was not duplicated or overwritten.");
                _db.AttendanceEvents.Add(attendanceEvent);
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(true, attendanceEvent.ProcessingStatus, attendanceEvent.ReviewReason!, attendanceEvent.Id, existingAttendance.Id, studentId);
            }

            attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.NeedsReview;
            attendanceEvent.ReviewReason = AppendReason(attendanceEvent.ReviewReason, $"Existing attendance is {existingAttendance.Status}; automatic integration did not overwrite it.");
            _db.AttendanceEvents.Add(attendanceEvent);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(false, attendanceEvent.ProcessingStatus, attendanceEvent.ReviewReason!, attendanceEvent.Id, existingAttendance.Id, studentId);
        }

        var attendance = new StudentAttendance
        {
            SchoolId = schoolId,
            AcademicSessionId = enrollment.AcademicSessionId,
            StudentId = studentId,
            StudentEnrollmentId = enrollment.Id,
            SchoolClassId = enrollment.SchoolClassId!.Value,
            SectionId = enrollment.SectionId,
            AttendanceDate = localDate,
            Status = StudentAttendanceStatus.Present,
            Source = source,
            Remarks = source == AttendanceSource.Camera ? "Marked through camera attendance." : "Marked from biometric attendance event.",
            MarkedByUserId = markedByUserId,
            MarkedAtUtc = DateTime.UtcNow
        };

        _db.StudentAttendances.Add(attendance);
        attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.AttendanceApplied;
        attendanceEvent.ProcessedAtUtc = DateTime.UtcNow;
        attendanceEvent.StudentAttendance = attendance;
        _db.AttendanceEvents.Add(attendanceEvent);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(true, attendanceEvent.ProcessingStatus, "Student marked Present successfully.", attendanceEvent.Id, attendance.Id, studentId);
    }

    private static string? Clean(string? value, int maxLength)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return clean is { Length: > 0 } && clean.Length > maxLength ? clean[..maxLength] : clean;
    }

    private static string AppendReason(string? existing, string addition)
        => string.IsNullOrWhiteSpace(existing) ? addition : $"{existing.Trim()} {addition}";

    private static decimal? NormalizeConfidence(decimal? value)
    {
        if (!value.HasValue) return null;
        return Math.Clamp(value.Value, 0m, 1m);
    }
}
