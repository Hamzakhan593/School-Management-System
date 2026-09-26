using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.HR)]
public class StaffAttendanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IStaffAttendanceService _attendance;
    private readonly IAuditService _audit;

    public StaffAttendanceController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IStaffAttendanceService attendance,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _attendance = attendance;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await _attendance.BuildDailyAsync(context.Value.SchoolId, date?.Date ?? DateTime.Today, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(StaffAttendanceMarkingViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please correct the staff attendance form and try again.";
            return RedirectToAction(nameof(Index), new { date = model.AttendanceDate.ToString("yyyy-MM-dd") });
        }

        var result = await _attendance.SaveDailyAsync(context.Value.SchoolId, context.Value.UserId, model, cancellationToken);
        if (!result.Success)
            TempData["Error"] = result.Message;
        else
        {
            await _audit.WriteAsync("StaffAttendance.DailySaved", "StaffAttendance", null,
                $"Date={model.AttendanceDate:yyyy-MM-dd}; Created={result.CreatedCount}; Updated={result.UpdatedCount}");
            TempData["Success"] = result.Message;
        }
        return RedirectToAction(nameof(Index), new { date = model.AttendanceDate.ToString("yyyy-MM-dd") });
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyReport(string? month, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var selectedMonth = DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? new DateTime(parsed.Year, parsed.Month, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var model = await _attendance.BuildMonthlyReportAsync(context.Value.SchoolId, selectedMonth, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Integrations(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = new StaffAttendanceIntegrationsViewModel
        {
            Devices = await _db.BiometricDevices.AsNoTracking()
                .Where(x => x.SchoolId == context.Value.SchoolId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken),
            Enrollments = await _db.StaffBiometricEnrollments.AsNoTracking()
                .Include(x => x.Staff)
                .Include(x => x.BiometricDevice)
                .Where(x => x.SchoolId == context.Value.SchoolId)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Staff.FullName)
                .ToListAsync(cancellationToken),
            StaffMembers = await _db.Staff.AsNoTracking()
                .Where(x => x.SchoolId == context.Value.SchoolId && (x.Status == StaffStatus.Active || x.Status == StaffStatus.OnLeave))
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollBiometric(StaffBiometricEnrollmentFormViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Complete the biometric enrollment form.";
            return RedirectToAction(nameof(Integrations));
        }

        var device = await _db.BiometricDevices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == context.Value.SchoolId && x.Id == model.BiometricDeviceId && x.IsActive, cancellationToken);
        var staff = await _db.Staff.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == context.Value.SchoolId && x.Id == model.StaffId, cancellationToken);
        if (device is null || staff is null)
        {
            TempData["Error"] = "Selected device or staff member is invalid.";
            return RedirectToAction(nameof(Integrations));
        }

        var reference = model.DeviceUserReference.Trim();
        var duplicate = await _db.StaffBiometricEnrollments.AnyAsync(x => x.SchoolId == context.Value.SchoolId
            && x.BiometricDeviceId == model.BiometricDeviceId && x.DeviceUserReference == reference && x.IsActive, cancellationToken);
        if (duplicate)
        {
            TempData["Error"] = "That device user reference is already assigned to an active staff enrollment on this device.";
            return RedirectToAction(nameof(Integrations));
        }

        _db.StaffBiometricEnrollments.Add(new StaffBiometricEnrollment
        {
            SchoolId = context.Value.SchoolId,
            BiometricDeviceId = model.BiometricDeviceId,
            StaffId = model.StaffId,
            DeviceUserReference = reference,
            Modality = model.Modality,
            TemplateReference = string.IsNullOrWhiteSpace(model.TemplateReference) ? null : model.TemplateReference.Trim(),
            IsActive = true,
            EnrolledAtUtc = DateTime.UtcNow,
            EnrolledByUserId = context.Value.UserId
        });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("StaffAttendance.BiometricEnrolled", "Staff", model.StaffId.ToString(), $"DeviceId={model.BiometricDeviceId}; Ref={reference}");
        TempData["Success"] = "Staff biometric enrollment saved.";
        return RedirectToAction(nameof(Integrations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateEnrollment(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var enrollment = await _db.StaffBiometricEnrollments
            .FirstOrDefaultAsync(x => x.SchoolId == context.Value.SchoolId && x.Id == id, cancellationToken);
        if (enrollment is null)
        {
            TempData["Error"] = "Enrollment was not found.";
            return RedirectToAction(nameof(Integrations));
        }
        enrollment.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("StaffAttendance.BiometricDeactivated", "StaffBiometricEnrollment", id.ToString());
        TempData["Success"] = "Enrollment deactivated.";
        return RedirectToAction(nameof(Integrations));
    }

    [HttpGet]
    public async Task<IActionResult> Camera(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = new StaffCameraAttendanceViewModel
        {
            StaffMembers = await _db.Staff.AsNoTracking()
                .Where(x => x.SchoolId == context.Value.SchoolId && (x.Status == StaffStatus.Active || x.Status == StaffStatus.OnLeave))
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CameraConfirm(StaffCameraAttendanceViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (model.StaffId <= 0)
        {
            TempData["Error"] = "Select the staff member whose identity you visually verified.";
            return RedirectToAction(nameof(Camera));
        }
        var result = await _attendance.MarkCameraVerifiedAsync(context.Value.SchoolId, context.Value.UserId, model.StaffId, DateTimeOffset.Now, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("StaffAttendance.CameraConfirmed", "Staff", model.StaffId.ToString(), $"EventId={result.EventId}");
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Camera));
    }

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is null || string.IsNullOrWhiteSpace(user.Id)) return null;
        return (user.SchoolId.Value, user.Id);
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school before using staff attendance.";
        return RedirectToAction("Index", "SchoolSetup");
    }
}
