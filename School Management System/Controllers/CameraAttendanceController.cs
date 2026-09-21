using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Options;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Teacher)]
public class CameraAttendanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IAttendanceIntegrationService _integration;
    private readonly ICameraRecognitionProvider _recognitionProvider;
    private readonly IAuditService _audit;
    private readonly AttendanceIntegrationOptions _options;

    public CameraAttendanceController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IAttendanceIntegrationService integration,
        ICameraRecognitionProvider recognitionProvider,
        IAuditService audit,
        IOptions<AttendanceIntegrationOptions> options)
    {
        _db = db;
        _schoolContext = schoolContext;
        _integration = integration;
        _recognitionProvider = recognitionProvider;
        _audit = audit;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var model = new CameraAttendanceViewModel();
        await PopulateStudentsAsync(model, context.Value.SchoolId, context.Value.UserId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(CameraAttendanceViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        if (!TryDecodeImage(model.CapturedImageDataUrl, out var imageBytes, out var decodeError))
        {
            ModelState.AddModelError(nameof(model.CapturedImageDataUrl), decodeError ?? "Capture a camera image first.");
            await PopulateStudentsAsync(model, context.Value.SchoolId, context.Value.UserId, cancellationToken);
            return View("Index", model);
        }

        if (imageBytes.Length > _options.MaxCameraImageBytes)
        {
            ModelState.AddModelError(nameof(model.CapturedImageDataUrl), $"Camera image is too large. Maximum is {_options.MaxCameraImageBytes / 1_000_000.0:0.0} MB.");
            await PopulateStudentsAsync(model, context.Value.SchoolId, context.Value.UserId, cancellationToken);
            return View("Index", model);
        }

        var recognition = await _recognitionProvider.RecognizeAsync(context.Value.SchoolId, imageBytes, cancellationToken);
        var occurredAt = _integration.GetSchoolLocalNow();
        var faceCount = recognition.IsConfigured ? recognition.FaceCount : model.ClientDetectedFaceCount;
        var recognizedProfileActive = recognition.StudentId.HasValue
            && await _db.CameraFaceEnrollments.AsNoTracking().AnyAsync(x =>
                x.SchoolId == context.Value.SchoolId
                && x.StudentId == recognition.StudentId.Value
                && x.ProviderName == recognition.ProviderName
                && x.IsActive,
                cancellationToken);

        var autoMatchAllowed = recognition.IsConfigured
            && recognition.FaceCount == 1
            && recognition.StudentId.HasValue
            && recognition.Confidence.HasValue
            && recognition.Confidence.Value >= _options.CameraAutoAcceptConfidence
            && recognizedProfileActive;

        AttendanceIntegrationResult result;
        if (autoMatchAllowed)
        {
            var canAccess = await CanProcessStudentAsync(context.Value.SchoolId, context.Value.UserId, recognition.StudentId!.Value, occurredAt.Date, cancellationToken);
            if (!canAccess)
            {
                var eventId = await _integration.RecordCameraReviewEventAsync(
                    context.Value.SchoolId, context.Value.UserId, occurredAt, recognition.StudentId,
                    recognition.Confidence, recognition.FaceCount,
                    "Recognition matched a student outside the current user's assigned classes; attendance was not changed.",
                    cancellationToken);
                result = new AttendanceIntegrationResult(false, AttendanceEventProcessingStatus.NeedsReview, $"Matched student is outside your assigned classes. Event #{eventId} logged for review.", eventId, null, recognition.StudentId);
            }
            else
            {
                result = await _integration.ProcessCameraAttendanceAsync(
                    context.Value.SchoolId,
                    context.Value.UserId,
                    recognition.StudentId.Value,
                    occurredAt,
                    recognition.Confidence,
                    recognition.FaceCount,
                    $"Automatic camera match by {recognition.ProviderName}.",
                    cancellationToken);
            }
        }
        else if (model.ManualStudentId.HasValue)
        {
            var canAccess = await CanProcessStudentAsync(context.Value.SchoolId, context.Value.UserId, model.ManualStudentId.Value, occurredAt.Date, cancellationToken);
            if (!canAccess)
            {
                var eventId = await _integration.RecordCameraReviewEventAsync(
                    context.Value.SchoolId, context.Value.UserId, occurredAt, model.ManualStudentId,
                    recognition.Confidence, faceCount,
                    "Manual camera confirmation attempted for a student outside the current user's assigned classes.",
                    cancellationToken);
                result = new AttendanceIntegrationResult(false, AttendanceEventProcessingStatus.NeedsReview, $"You are not assigned to mark this student's attendance. Event #{eventId} logged.", eventId, null, model.ManualStudentId);
            }
            else
            {
                var reason = recognition.IsConfigured
                    ? $"Manual review confirmed. Provider result: {recognition.Message}"
                    : "Manual camera verification used because automated face recognition is not configured.";

                result = await _integration.ProcessCameraAttendanceAsync(
                    context.Value.SchoolId,
                    context.Value.UserId,
                    model.ManualStudentId.Value,
                    occurredAt,
                    recognition.Confidence,
                    faceCount,
                    reason,
                    cancellationToken);
            }
        }
        else
        {
            var reason = recognition.IsConfigured
                ? $"Camera match requires manual review. {recognition.Message}"
                : recognition.Message;

            var eventId = await _integration.RecordCameraReviewEventAsync(
                context.Value.SchoolId,
                context.Value.UserId,
                occurredAt,
                recognition.StudentId,
                recognition.Confidence,
                faceCount,
                reason,
                cancellationToken);

            result = new AttendanceIntegrationResult(false, AttendanceEventProcessingStatus.NeedsReview, $"{reason} Select the student and submit again. Event #{eventId} logged for review.", eventId, null, recognition.StudentId);
        }

        await _audit.WriteAsync(
            "AttendanceIntegration.CameraProcessed",
            nameof(AttendanceEvent),
            result.EventId?.ToString(),
            $"Status={result.Status}; StudentId={result.StudentId}; AttendanceId={result.AttendanceId}; Provider={recognition.ProviderName}");

        model.ResultSuccess = result.Success;
        model.ResultMessage = result.Message;
        model.CapturedImageDataUrl = null;
        await PopulateStudentsAsync(model, context.Value.SchoolId, context.Value.UserId, cancellationToken);
        return View("Index", model);
    }

    [Authorize(Roles = AppRoles.UserManagers)]
    [HttpGet]
    public async Task<IActionResult> Enroll(int? studentId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var model = new CameraEnrollmentViewModel { StudentId = studentId ?? 0 };
        await PopulateEnrollmentStudentsAsync(model, context.Value.SchoolId, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = AppRoles.UserManagers)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(CameraEnrollmentViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var validStudent = await _db.Students.AsNoTracking()
            .AnyAsync(x => x.Id == model.StudentId && x.SchoolId == context.Value.SchoolId && x.Status == StudentStatus.Active, cancellationToken);
        if (!validStudent)
            ModelState.AddModelError(nameof(model.StudentId), "Select an active student.");

        var samples = new List<byte[]>();
        foreach (var dataUrl in new[] { model.Sample1DataUrl, model.Sample2DataUrl, model.Sample3DataUrl })
        {
            if (!TryDecodeImage(dataUrl, out var bytes, out _))
                continue;
            if (bytes.Length <= _options.MaxCameraImageBytes)
                samples.Add(bytes);
        }

        if (samples.Count < 3)
            ModelState.AddModelError(string.Empty, "Capture all 3 face samples before enrollment.");

        if (!ModelState.IsValid)
        {
            await PopulateEnrollmentStudentsAsync(model, context.Value.SchoolId, cancellationToken);
            return View(model);
        }

        var result = await _recognitionProvider.EnrollAsync(context.Value.SchoolId, model.StudentId, samples, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.TemplateReference))
        {
            ModelState.AddModelError(string.Empty, result.Message);
            // Never return captured samples back into the page after processing.
            model.Sample1DataUrl = model.Sample2DataUrl = model.Sample3DataUrl = null;
            await PopulateEnrollmentStudentsAsync(model, context.Value.SchoolId, cancellationToken);
            return View(model);
        }

        var existing = await _db.CameraFaceEnrollments
            .FirstOrDefaultAsync(x => x.SchoolId == context.Value.SchoolId
                && x.StudentId == model.StudentId
                && x.ProviderName == result.ProviderName,
                cancellationToken);

        if (existing is null)
        {
            _db.CameraFaceEnrollments.Add(new CameraFaceEnrollment
            {
                SchoolId = context.Value.SchoolId,
                StudentId = model.StudentId,
                ProviderName = result.ProviderName,
                TemplateReference = result.TemplateReference,
                IsActive = true,
                EnrolledByUserId = context.Value.UserId
            });
        }
        else
        {
            existing.TemplateReference = result.TemplateReference;
            existing.IsActive = true;
            existing.EnrolledAtUtc = DateTime.UtcNow;
            existing.EnrolledByUserId = context.Value.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("AttendanceIntegration.CameraEnrollmentSaved", nameof(CameraFaceEnrollment), model.StudentId.ToString(), $"Provider={result.ProviderName}");

        TempData["Success"] = "Camera face enrollment saved. Raw camera samples were not stored by the school application.";
        return RedirectToAction(nameof(AttendanceIntegrationsController.Index), "AttendanceIntegrations");
    }

    private async Task PopulateStudentsAsync(CameraAttendanceViewModel model, int schoolId, string currentUserId, CancellationToken cancellationToken)
    {
        IQueryable<Student> students = _db.Students.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status == StudentStatus.Active);

        if (!IsManager())
        {
            var activeSessionId = await _db.AcademicSessions.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!activeSessionId.HasValue)
            {
                model.Students = [];
                return;
            }

            var classWideIds = await _db.TeacherAssignments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == activeSessionId.Value
                    && x.TeacherUserId == currentUserId && x.IsActive && x.SectionId == null)
                .Select(x => x.SchoolClassId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var sectionIds = await _db.TeacherAssignments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == activeSessionId.Value
                    && x.TeacherUserId == currentUserId && x.IsActive && x.SectionId != null)
                .Select(x => x.SectionId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var classTeacherSectionIds = await _db.Sections.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.ClassTeacherUserId == currentUserId && x.IsActive)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            sectionIds = sectionIds.Concat(classTeacherSectionIds).Distinct().ToList();

            var allowedStudentIds = await _db.StudentEnrollments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == activeSessionId.Value
                    && x.IsCurrent && x.SchoolClassId != null
                    && (classWideIds.Contains(x.SchoolClassId.Value)
                        || (x.SectionId != null && sectionIds.Contains(x.SectionId.Value))))
                .Select(x => x.StudentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            students = students.Where(x => allowedStudentIds.Contains(x.Id));
        }

        model.Students = await students
            .OrderBy(x => x.FullName)
            .Select(x => new SelectListItem($"{x.FullName} - {x.AdmissionNumber}", x.Id.ToString()))
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> CanProcessStudentAsync(int schoolId, string currentUserId, int studentId, DateTime localDate, CancellationToken cancellationToken)
    {
        if (IsManager())
            return await _db.Students.AsNoTracking().AnyAsync(x => x.Id == studentId && x.SchoolId == schoolId && x.Status == StudentStatus.Active, cancellationToken);

        var enrollment = await _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.StudentId == studentId && x.SchoolClassId != null
                && x.EffectiveFrom <= localDate && (x.EffectiveTo == null || x.EffectiveTo >= localDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (enrollment is null) return false;

        var assigned = await _db.TeacherAssignments.AsNoTracking().AnyAsync(x =>
            x.SchoolId == schoolId
            && x.AcademicSessionId == enrollment.AcademicSessionId
            && x.SchoolClassId == enrollment.SchoolClassId.Value
            && x.TeacherUserId == currentUserId
            && x.IsActive
            && (x.SectionId == null || x.SectionId == enrollment.SectionId),
            cancellationToken);
        if (assigned) return true;

        return enrollment.SectionId.HasValue && await _db.Sections.AsNoTracking().AnyAsync(x =>
            x.Id == enrollment.SectionId.Value && x.SchoolId == schoolId
            && x.ClassTeacherUserId == currentUserId && x.IsActive,
            cancellationToken);
    }

    private bool IsManager()
        => User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin);

    private async Task PopulateEnrollmentStudentsAsync(CameraEnrollmentViewModel model, int schoolId, CancellationToken cancellationToken)
    {
        model.Students = await _db.Students.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status == StudentStatus.Active)
            .OrderBy(x => x.FullName)
            .Select(x => new SelectListItem($"{x.FullName} - {x.AdmissionNumber}", x.Id.ToString()))
            .ToListAsync(cancellationToken);
    }

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is null || string.IsNullOrWhiteSpace(user.Id)) return null;
        return (user.SchoolId.Value, user.Id);
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school first.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private static bool TryDecodeImage(string? dataUrl, out byte[] bytes, out string? error)
    {
        bytes = [];
        error = null;
        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            error = "Capture a camera image first.";
            return false;
        }

        var comma = dataUrl.IndexOf(',');
        if (comma <= 0 || !dataUrl[..comma].Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            error = "Invalid camera image format.";
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(dataUrl[(comma + 1)..]);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            error = "Invalid camera image data.";
            return false;
        }
    }
}
