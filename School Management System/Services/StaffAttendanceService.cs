using System.Data;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class StaffAttendanceService : IStaffAttendanceService
{
    private readonly ApplicationDbContext _db;

    public StaffAttendanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StaffAttendanceMarkingViewModel> BuildDailyAsync(int schoolId, DateTime date, CancellationToken cancellationToken = default)
    {
        date = date.Date;
        var staff = await _db.Staff.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                && x.JoiningDate <= date
                && (!x.ExitDate.HasValue || x.ExitDate.Value >= date)
                && x.Status != StaffStatus.Inactive)
            .OrderBy(x => x.Department)
            .ThenBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        var ids = staff.Select(x => x.Id).ToList();
        var existing = await _db.StaffAttendances.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.AttendanceDate == date && ids.Contains(x.StaffId))
            .ToDictionaryAsync(x => x.StaffId, cancellationToken);

        return new StaffAttendanceMarkingViewModel
        {
            AttendanceDate = date,
            StaffMembers = staff.Select(s =>
            {
                existing.TryGetValue(s.Id, out var a);
                return new StaffAttendanceRowViewModel
                {
                    StaffId = s.Id,
                    EmployeeId = s.EmployeeId,
                    StaffName = s.FullName,
                    Designation = s.Designation,
                    Department = s.Department,
                    Status = a?.Status ?? StaffAttendanceStatus.Present,
                    Source = a?.Source ?? AttendanceSource.Manual,
                    CheckInUtc = a?.CheckInUtc,
                    CheckOutUtc = a?.CheckOutUtc,
                    Remarks = a?.Remarks,
                    AlreadySaved = a is not null
                };
            }).ToList()
        };
    }

    public async Task<StaffAttendanceSaveResult> SaveDailyAsync(
        int schoolId,
        string userId,
        StaffAttendanceMarkingViewModel model,
        CancellationToken cancellationToken = default)
    {
        var date = model.AttendanceDate.Date;
        var postedIds = model.StaffMembers.Select(x => x.StaffId).Distinct().ToList();
        if (postedIds.Count == 0)
            return new StaffAttendanceSaveResult(false, "No staff attendance rows were submitted.");

        var validStaff = await _db.Staff.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && postedIds.Contains(x.Id)
                && x.JoiningDate <= date
                && (!x.ExitDate.HasValue || x.ExitDate.Value >= date))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (validStaff.Count != postedIds.Count)
            return new StaffAttendanceSaveResult(false, "One or more submitted staff records do not belong to this school or were not employed on the selected date.");

        var existing = await _db.StaffAttendances
            .Where(x => x.SchoolId == schoolId && x.AttendanceDate == date && postedIds.Contains(x.StaffId))
            .ToDictionaryAsync(x => x.StaffId, cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var row in model.StaffMembers)
        {
            if (!existing.TryGetValue(row.StaffId, out var attendance))
            {
                attendance = new StaffAttendance
                {
                    SchoolId = schoolId,
                    StaffId = row.StaffId,
                    AttendanceDate = date,
                    Status = row.Status,
                    Source = AttendanceSource.Manual,
                    Remarks = Clean(row.Remarks, 300),
                    MarkedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _db.StaffAttendances.Add(attendance);
                created++;
            }
            else
            {
                var statusChanged = attendance.Status != row.Status;
                attendance.Status = row.Status;
                if (statusChanged || attendance.Source == AttendanceSource.Manual)
                    attendance.Source = AttendanceSource.Manual;
                attendance.Remarks = Clean(row.Remarks, 300);
                attendance.MarkedByUserId = userId;
                attendance.UpdatedAtUtc = DateTime.UtcNow;
                updated++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new StaffAttendanceSaveResult(true, $"Staff attendance saved for {date:dd MMM yyyy}.", created, updated);
    }

    public async Task<StaffAttendanceMonthlyReportViewModel> BuildMonthlyReportAsync(
        int schoolId,
        DateTime month,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        var staff = await _db.Staff.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                && x.JoiningDate < end
                && (!x.ExitDate.HasValue || x.ExitDate.Value >= start))
            .OrderBy(x => x.Department)
            .ThenBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        var ids = staff.Select(x => x.Id).ToList();
        var records = await _db.StaffAttendances.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.AttendanceDate >= start && x.AttendanceDate < end && ids.Contains(x.StaffId))
            .ToListAsync(cancellationToken);
        var byStaff = records.GroupBy(x => x.StaffId).ToDictionary(x => x.Key, x => x.ToList());

        return new StaffAttendanceMonthlyReportViewModel
        {
            Month = start,
            Rows = staff.Select(s =>
            {
                var list = byStaff.TryGetValue(s.Id, out var found) ? found : new List<StaffAttendance>();
                return new StaffAttendanceMonthlyRowViewModel
                {
                    StaffId = s.Id,
                    EmployeeId = s.EmployeeId,
                    StaffName = s.FullName,
                    Department = s.Department,
                    PresentDays = list.Count(x => x.Status == StaffAttendanceStatus.Present),
                    AbsentDays = list.Count(x => x.Status == StaffAttendanceStatus.Absent),
                    LateDays = list.Count(x => x.Status == StaffAttendanceStatus.Late),
                    LeaveDays = list.Count(x => x.Status == StaffAttendanceStatus.Leave),
                    HalfDays = list.Count(x => x.Status == StaffAttendanceStatus.HalfDay),
                    HolidayDays = list.Count(x => x.Status == StaffAttendanceStatus.Holiday),
                    TotalMarkedDays = list.Count
                };
            }).ToList()
        };
    }

    public async Task<StaffAttendanceIntegrationResult> ProcessBiometricEventAsync(
        BiometricDevice device,
        BridgeAttendanceEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var externalId = request.EventId.Trim();
        var userReference = request.DeviceUserReference.Trim();
        var occurredAt = request.OccurredAt == default ? DateTimeOffset.UtcNow : request.OccurredAt;
        var localDate = occurredAt.Date;

        var exactExisting = await _db.StaffAttendanceEvents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BiometricDeviceId == device.Id && x.ExternalEventId == externalId, cancellationToken);
        if (exactExisting is not null)
        {
            return new StaffAttendanceIntegrationResult(
                true,
                exactExisting.ProcessingStatus,
                "This staff attendance event was already received.",
                exactExisting.Id,
                exactExisting.StaffAttendanceId,
                exactExisting.StaffId);
        }

        var enrollment = await _db.StaffBiometricEnrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == device.SchoolId
                && x.BiometricDeviceId == device.Id
                && x.DeviceUserReference == userReference
                && x.IsActive,
                cancellationToken);

        if (enrollment is null)
        {
            var unmatched = new StaffAttendanceEvent
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
                ReviewReason = "No active staff biometric enrollment matches this device user reference.",
                RawReference = Clean(request.RawReference, 250),
                ProcessedAtUtc = DateTime.UtcNow
            };
            _db.StaffAttendanceEvents.Add(unmatched);
            await _db.SaveChangesAsync(cancellationToken);
            return new StaffAttendanceIntegrationResult(false, unmatched.ProcessingStatus, unmatched.ReviewReason!, unmatched.Id);
        }

        var duplicateStart = occurredAt.UtcDateTime.AddSeconds(-30);
        var repeated = await _db.StaffAttendanceEvents.AsNoTracking()
            .Where(x => x.SchoolId == device.SchoolId
                && x.BiometricDeviceId == device.Id
                && x.StaffId == enrollment.StaffId
                && x.Direction == request.Direction
                && x.OccurredAtUtc >= duplicateStart
                && x.OccurredAtUtc <= occurredAt.UtcDateTime
                && x.ProcessingStatus != AttendanceEventProcessingStatus.Duplicate
                && x.ProcessingStatus != AttendanceEventProcessingStatus.Rejected)
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (repeated is not null)
        {
            var duplicate = new StaffAttendanceEvent
            {
                SchoolId = device.SchoolId,
                StaffId = enrollment.StaffId,
                BiometricDeviceId = device.Id,
                ExternalEventId = externalId,
                DeviceUserReference = userReference,
                OccurredAtUtc = occurredAt.UtcDateTime,
                LocalDate = localDate,
                Source = AttendanceSource.Biometric,
                Direction = request.Direction,
                ProcessingStatus = AttendanceEventProcessingStatus.Duplicate,
                ReviewReason = "Repeated staff scan inside the 30-second de-duplication window.",
                RawReference = Clean(request.RawReference, 250),
                ProcessedAtUtc = DateTime.UtcNow
            };
            _db.StaffAttendanceEvents.Add(duplicate);
            await _db.SaveChangesAsync(cancellationToken);
            return new StaffAttendanceIntegrationResult(true, duplicate.ProcessingStatus, duplicate.ReviewReason!, duplicate.Id, null, enrollment.StaffId);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var attendance = await _db.StaffAttendances
            .FirstOrDefaultAsync(x => x.SchoolId == device.SchoolId && x.StaffId == enrollment.StaffId && x.AttendanceDate == localDate, cancellationToken);

        var attendanceEvent = new StaffAttendanceEvent
        {
            SchoolId = device.SchoolId,
            StaffId = enrollment.StaffId,
            BiometricDeviceId = device.Id,
            ExternalEventId = externalId,
            DeviceUserReference = userReference,
            OccurredAtUtc = occurredAt.UtcDateTime,
            LocalDate = localDate,
            Source = AttendanceSource.Biometric,
            Direction = request.Direction,
            RawReference = Clean(request.RawReference, 250)
        };

        if (attendance is null)
        {
            attendance = new StaffAttendance
            {
                SchoolId = device.SchoolId,
                StaffId = enrollment.StaffId,
                AttendanceDate = localDate,
                Status = StaffAttendanceStatus.Present,
                Source = AttendanceSource.Biometric,
                CreatedAtUtc = DateTime.UtcNow
            };
            if (request.Direction != AttendanceDirection.Out)
                attendance.CheckInUtc = occurredAt.UtcDateTime;
            else
                attendance.CheckOutUtc = occurredAt.UtcDateTime;
            _db.StaffAttendances.Add(attendance);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (attendance.Status is StaffAttendanceStatus.Leave or StaffAttendanceStatus.Holiday)
        {
            attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.NeedsReview;
            attendanceEvent.ReviewReason = $"Staff scan received while attendance status is {attendance.Status}. Manual review is required.";
            attendanceEvent.ProcessedAtUtc = DateTime.UtcNow;
            _db.StaffAttendanceEvents.Add(attendanceEvent);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new StaffAttendanceIntegrationResult(false, attendanceEvent.ProcessingStatus, attendanceEvent.ReviewReason, attendanceEvent.Id, attendance.Id, enrollment.StaffId);
        }
        else
        {
            if (attendance.Status == StaffAttendanceStatus.Absent)
                attendance.Status = StaffAttendanceStatus.Present;
            attendance.Source = AttendanceSource.Biometric;
            attendance.UpdatedAtUtc = DateTime.UtcNow;
            if (request.Direction == AttendanceDirection.Out)
            {
                if (!attendance.CheckOutUtc.HasValue || occurredAt.UtcDateTime > attendance.CheckOutUtc.Value)
                    attendance.CheckOutUtc = occurredAt.UtcDateTime;
            }
            else
            {
                if (!attendance.CheckInUtc.HasValue || occurredAt.UtcDateTime < attendance.CheckInUtc.Value)
                    attendance.CheckInUtc = occurredAt.UtcDateTime;
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        attendanceEvent.StaffAttendanceId = attendance.Id;
        attendanceEvent.ProcessingStatus = AttendanceEventProcessingStatus.AttendanceApplied;
        attendanceEvent.ProcessedAtUtc = DateTime.UtcNow;
        _db.StaffAttendanceEvents.Add(attendanceEvent);
        device.LastSyncAtUtc = DateTime.UtcNow;
        device.LastStatusMessage = "Connected - staff attendance event processed";
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StaffAttendanceIntegrationResult(true, attendanceEvent.ProcessingStatus, "Staff attendance event applied.", attendanceEvent.Id, attendance.Id, enrollment.StaffId);
    }

    public async Task<StaffAttendanceIntegrationResult> MarkCameraVerifiedAsync(
        int schoolId,
        string userId,
        int staffId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        var staffExists = await _db.Staff.AsNoTracking().AnyAsync(x => x.SchoolId == schoolId && x.Id == staffId, cancellationToken);
        if (!staffExists)
            return new StaffAttendanceIntegrationResult(false, AttendanceEventProcessingStatus.Rejected, "Staff record was not found.");

        var localDate = occurredAt.Date;
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var attendance = await _db.StaffAttendances
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.StaffId == staffId && x.AttendanceDate == localDate, cancellationToken);

        if (attendance is null)
        {
            attendance = new StaffAttendance
            {
                SchoolId = schoolId,
                StaffId = staffId,
                AttendanceDate = localDate,
                Status = StaffAttendanceStatus.Present,
                Source = AttendanceSource.Camera,
                CheckInUtc = occurredAt.UtcDateTime,
                MarkedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.StaffAttendances.Add(attendance);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (attendance.Status is StaffAttendanceStatus.Leave or StaffAttendanceStatus.Holiday)
        {
            var review = new StaffAttendanceEvent
            {
                SchoolId = schoolId,
                StaffId = staffId,
                StaffAttendanceId = attendance.Id,
                OccurredAtUtc = occurredAt.UtcDateTime,
                LocalDate = localDate,
                Source = AttendanceSource.Camera,
                Direction = AttendanceDirection.In,
                ProcessingStatus = AttendanceEventProcessingStatus.NeedsReview,
                ReviewReason = $"Camera confirmation received while status is {attendance.Status}.",
                ProcessedAtUtc = DateTime.UtcNow
            };
            _db.StaffAttendanceEvents.Add(review);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new StaffAttendanceIntegrationResult(false, review.ProcessingStatus, review.ReviewReason!, review.Id, attendance.Id, staffId);
        }
        else
        {
            if (attendance.Status == StaffAttendanceStatus.Absent) attendance.Status = StaffAttendanceStatus.Present;
            attendance.Source = AttendanceSource.Camera;
            attendance.MarkedByUserId = userId;
            if (!attendance.CheckInUtc.HasValue || occurredAt.UtcDateTime < attendance.CheckInUtc.Value) attendance.CheckInUtc = occurredAt.UtcDateTime;
            attendance.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var attendanceEvent = new StaffAttendanceEvent
        {
            SchoolId = schoolId,
            StaffId = staffId,
            StaffAttendanceId = attendance.Id,
            OccurredAtUtc = occurredAt.UtcDateTime,
            LocalDate = localDate,
            Source = AttendanceSource.Camera,
            Direction = AttendanceDirection.In,
            ProcessingStatus = AttendanceEventProcessingStatus.AttendanceApplied,
            ReviewReason = "Identity visually confirmed by an authorized staff user.",
            ProcessedAtUtc = DateTime.UtcNow
        };
        _db.StaffAttendanceEvents.Add(attendanceEvent);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new StaffAttendanceIntegrationResult(true, attendanceEvent.ProcessingStatus, "Camera attendance saved after authorized visual confirmation.", attendanceEvent.Id, attendance.Id, staffId);
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
