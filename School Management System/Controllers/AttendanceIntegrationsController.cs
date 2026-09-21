using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.UserManagers)]
public class AttendanceIntegrationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IAuditService _audit;

    public AttendanceIntegrationsController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var model = new AttendanceIntegrationsIndexViewModel
        {
            Devices = await _db.BiometricDevices.AsNoTracking()
                .Where(x => x.SchoolId == schoolId.Value)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken),
            Enrollments = await _db.BiometricEnrollments.AsNoTracking()
                .Include(x => x.BiometricDevice)
                .Include(x => x.Student)
                .Where(x => x.SchoolId == schoolId.Value)
                .OrderBy(x => x.BiometricDevice.Name)
                .ThenBy(x => x.Student.FullName)
                .Take(250)
                .ToListAsync(cancellationToken),
            CameraEnrollments = await _db.CameraFaceEnrollments.AsNoTracking()
                .Include(x => x.Student)
                .Where(x => x.SchoolId == schoolId.Value && x.IsActive)
                .OrderBy(x => x.Student.FullName)
                .Take(250)
                .ToListAsync(cancellationToken),
            RecentEvents = await _db.AttendanceEvents.AsNoTracking()
                .Include(x => x.Student)
                .Include(x => x.BiometricDevice)
                .Where(x => x.SchoolId == schoolId.Value)
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken),
            OneTimeDeviceKey = TempData["M07.DeviceKey"] as string,
            OneTimeDeviceCode = TempData["M07.DeviceCode"] as string
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateDevice()
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        return View("DeviceForm", new BiometricDeviceFormViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDevice(BiometricDeviceFormViewModel model, CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        NormalizeDeviceModel(model);
        if (await _db.BiometricDevices.AnyAsync(x => x.DeviceCode == model.DeviceCode, cancellationToken))
            ModelState.AddModelError(nameof(model.DeviceCode), "This device code is already in use.");

        if (!ModelState.IsValid)
            return View("DeviceForm", model);

        var plainKey = DeviceKeyUtility.Generate();
        var device = new BiometricDevice
        {
            SchoolId = schoolId.Value,
            DeviceCode = model.DeviceCode,
            Name = model.Name.Trim(),
            Model = Clean(model.Model),
            ConnectionType = Clean(model.ConnectionType),
            IpAddress = Clean(model.IpAddress),
            Location = Clean(model.Location),
            ApiKeyHash = DeviceKeyUtility.Hash(plainKey),
            IsActive = model.IsActive
        };

        _db.BiometricDevices.Add(device);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("AttendanceIntegration.DeviceCreated", nameof(BiometricDevice), device.Id.ToString(), $"Code={device.DeviceCode}; Name={device.Name}");

        TempData["M07.DeviceKey"] = plainKey;
        TempData["M07.DeviceCode"] = device.DeviceCode;
        TempData["Success"] = "Biometric device created. Copy the integration key now; it is shown only once.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditDevice(int id, CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var device = await _db.BiometricDevices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (device is null) return NotFound();

        return View("DeviceForm", new BiometricDeviceFormViewModel
        {
            Id = device.Id,
            DeviceCode = device.DeviceCode,
            Name = device.Name,
            Model = device.Model,
            ConnectionType = device.ConnectionType,
            IpAddress = device.IpAddress,
            Location = device.Location,
            IsActive = device.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDevice(BiometricDeviceFormViewModel model, CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        if (!model.Id.HasValue) return BadRequest();

        NormalizeDeviceModel(model);
        if (await _db.BiometricDevices.AnyAsync(x => x.DeviceCode == model.DeviceCode && x.Id != model.Id.Value, cancellationToken))
            ModelState.AddModelError(nameof(model.DeviceCode), "This device code is already in use.");

        if (!ModelState.IsValid)
            return View("DeviceForm", model);

        var device = await _db.BiometricDevices
            .FirstOrDefaultAsync(x => x.Id == model.Id.Value && x.SchoolId == schoolId.Value, cancellationToken);
        if (device is null) return NotFound();

        device.DeviceCode = model.DeviceCode;
        device.Name = model.Name.Trim();
        device.Model = Clean(model.Model);
        device.ConnectionType = Clean(model.ConnectionType);
        device.IpAddress = Clean(model.IpAddress);
        device.Location = Clean(model.Location);
        device.IsActive = model.IsActive;
        device.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("AttendanceIntegration.DeviceUpdated", nameof(BiometricDevice), device.Id.ToString(), $"Code={device.DeviceCode}; Active={device.IsActive}");
        TempData["Success"] = "Device updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateDeviceKey(int id, CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var device = await _db.BiometricDevices
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (device is null) return NotFound();

        var plainKey = DeviceKeyUtility.Generate();
        device.ApiKeyHash = DeviceKeyUtility.Hash(plainKey);
        device.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("AttendanceIntegration.DeviceKeyRegenerated", nameof(BiometricDevice), device.Id.ToString(), $"Code={device.DeviceCode}");

        TempData["M07.DeviceKey"] = plainKey;
        TempData["M07.DeviceCode"] = device.DeviceCode;
        TempData["Success"] = "New device key generated. The old key no longer works.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateEnrollment(int? deviceId, CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var model = new BiometricEnrollmentFormViewModel
        {
            BiometricDeviceId = deviceId ?? 0
        };
        await PopulateEnrollmentListsAsync(model, schoolId.Value, cancellationToken);
        return View("EnrollmentForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEnrollment(BiometricEnrollmentFormViewModel model, CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        model.DeviceUserReference = model.DeviceUserReference?.Trim() ?? string.Empty;
        model.TemplateReference = Clean(model.TemplateReference);

        var deviceValid = await _db.BiometricDevices.AnyAsync(x => x.Id == model.BiometricDeviceId && x.SchoolId == schoolId.Value && x.IsActive, cancellationToken);
        var studentValid = await _db.Students.AnyAsync(x => x.Id == model.StudentId && x.SchoolId == schoolId.Value && x.Status == StudentStatus.Active, cancellationToken);

        if (!deviceValid) ModelState.AddModelError(nameof(model.BiometricDeviceId), "Select an active device.");
        if (!studentValid) ModelState.AddModelError(nameof(model.StudentId), "Select an active student.");

        if (await _db.BiometricEnrollments.AnyAsync(x => x.BiometricDeviceId == model.BiometricDeviceId && x.DeviceUserReference == model.DeviceUserReference, cancellationToken))
            ModelState.AddModelError(nameof(model.DeviceUserReference), "This device user reference is already mapped on the selected device.");

        if (!ModelState.IsValid)
        {
            await PopulateEnrollmentListsAsync(model, schoolId.Value, cancellationToken);
            return View("EnrollmentForm", model);
        }

        var user = await _schoolContext.GetCurrentUserAsync();
        var enrollment = new BiometricEnrollment
        {
            SchoolId = schoolId.Value,
            BiometricDeviceId = model.BiometricDeviceId,
            StudentId = model.StudentId,
            DeviceUserReference = model.DeviceUserReference,
            Modality = model.Modality,
            TemplateReference = model.TemplateReference,
            IsActive = true,
            EnrolledByUserId = user?.Id
        };

        _db.BiometricEnrollments.Add(enrollment);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("AttendanceIntegration.BiometricEnrollmentCreated", nameof(BiometricEnrollment), enrollment.Id.ToString(), $"DeviceId={enrollment.BiometricDeviceId}; StudentId={enrollment.StudentId}; Ref={enrollment.DeviceUserReference}");

        TempData["Success"] = "Biometric user mapped to student.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEnrollment(int id, CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var enrollment = await _db.BiometricEnrollments
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (enrollment is null) return NotFound();

        enrollment.IsActive = !enrollment.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("AttendanceIntegration.BiometricEnrollmentToggled", nameof(BiometricEnrollment), enrollment.Id.ToString(), $"Active={enrollment.IsActive}");
        TempData["Success"] = enrollment.IsActive ? "Enrollment activated." : "Enrollment deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateEnrollmentListsAsync(BiometricEnrollmentFormViewModel model, int schoolId, CancellationToken cancellationToken)
    {
        model.Devices = await _db.BiometricDevices.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Name} ({x.DeviceCode})", x.Id.ToString()))
            .ToListAsync(cancellationToken);

        model.Students = await _db.Students.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status == StudentStatus.Active)
            .OrderBy(x => x.FullName)
            .Select(x => new SelectListItem($"{x.FullName} - {x.AdmissionNumber}", x.Id.ToString()))
            .ToListAsync(cancellationToken);
    }

    private async Task<int?> GetSchoolIdAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        return user?.SchoolId;
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school first.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private static void NormalizeDeviceModel(BiometricDeviceFormViewModel model)
    {
        model.DeviceCode = (model.DeviceCode ?? string.Empty).Trim().ToUpperInvariant();
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Model = Clean(model.Model);
        model.ConnectionType = Clean(model.ConnectionType);
        model.IpAddress = Clean(model.IpAddress);
        model.Location = Clean(model.Location);
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
