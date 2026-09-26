using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Teacher)]
public class AttendanceController : Controller
{
    private readonly ISchoolContextService _schoolContext;
    private readonly IAttendanceService _attendanceService;
    private readonly IAuditService _audit;

    public AttendanceController(
        ISchoolContextService schoolContext,
        IAttendanceService attendanceService,
        IAuditService audit)
    {
        _schoolContext = schoolContext;
        _attendanceService = attendanceService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? academicSessionId,
        int? schoolClassId,
        int? sectionId,
        DateTime? attendanceDate,
        CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var model = await _attendanceService.BuildMarkingSheetAsync(
            context.Value.SchoolId,
            context.Value.UserId,
            IsManager(),
            academicSessionId,
            schoolClassId,
            sectionId,
            attendanceDate?.Date ?? DateTime.Today,
            cancellationToken);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Sections(int academicSessionId, int schoolClassId, DateTime? attendanceDate, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return Json(Array.Empty<object>());
        var sheet = await _attendanceService.BuildMarkingSheetAsync(context.Value.SchoolId, context.Value.UserId, IsManager(), academicSessionId, schoolClassId, null, attendanceDate ?? DateTime.Today, cancellationToken);
        return Json(sheet.Sections.Select(x => new { x.Id, x.Name }));
    }

    private async Task<IActionResult> RedisplayAttendance(AttendanceMarkingViewModel posted, int schoolId, string userId, CancellationToken ct)
    {
        if (Request.HasFormContentType && Request.Form.Any(x => x.Key.EndsWith(".Status") && x.Value == "0"))
            ModelState.AddModelError(string.Empty, "Some students are unmarked. Choose a status for every student before saving.");
        var roster = await _attendanceService.BuildMarkingSheetAsync(schoolId, userId, IsManager(), posted.AcademicSessionId, posted.SchoolClassId, posted.SectionId, posted.AttendanceDate, ct);
        foreach (var row in roster.Students)
        {
            var input = posted.Students.FirstOrDefault(x => x.StudentId == row.StudentId);
            if (input is not null) { row.Status = input.Status; row.Remarks = input.Remarks; }
        }
        roster.CorrectionReason = posted.CorrectionReason;
        return View("Index", roster);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceMarkingViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        if (!ModelState.IsValid) return await RedisplayAttendance(model, context.Value.SchoolId, context.Value.UserId, cancellationToken);

        if (model.AcademicSessionId <= 0 || model.SchoolClassId <= 0)
        {
            TempData["Error"] = "Select an academic session and class before saving attendance.";
            return RedirectToAction(nameof(Index), new
            {
                academicSessionId = model.AcademicSessionId,
                schoolClassId = model.SchoolClassId,
                sectionId = model.SectionId,
                attendanceDate = model.AttendanceDate.ToString("yyyy-MM-dd")
            });
        }

        var result = await _attendanceService.SaveClassAttendanceAsync(
            context.Value.SchoolId,
            context.Value.UserId,
            IsManager(),
            model,
            cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await RedisplayAttendance(model, context.Value.SchoolId, context.Value.UserId, cancellationToken);
        }
        else
        {
            var details = $"Date={model.AttendanceDate:yyyy-MM-dd}; ClassId={model.SchoolClassId}; SectionId={model.SectionId?.ToString() ?? "none"}; Created={result.CreatedCount}; Updated={result.UpdatedCount}";
            if (result.WasLateCorrection)
                details += $"; LateCorrectionReason={model.CorrectionReason?.Trim()}";

            await _audit.WriteAsync(
                result.WasLateCorrection ? "Attendance.LateCorrectionSaved" : "Attendance.ClassSaved",
                "StudentAttendance",
                null,
                details);

            TempData["Success"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            academicSessionId = model.AcademicSessionId,
            schoolClassId = model.SchoolClassId,
            sectionId = model.SectionId,
            attendanceDate = model.AttendanceDate.ToString("yyyy-MM-dd")
        });
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyReport(
        int? academicSessionId,
        int? schoolClassId,
        int? sectionId,
        string? month,
        CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var selectedMonth = DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth)
            ? new DateTime(parsedMonth.Year, parsedMonth.Month, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        var model = await _attendanceService.BuildMonthlyReportAsync(
            context.Value.SchoolId,
            context.Value.UserId,
            IsManager(),
            academicSessionId,
            schoolClassId,
            sectionId,
            selectedMonth,
            cancellationToken);

        return View(model);
    }

    private bool IsManager()
        => User.IsInRole(AppRoles.SuperAdmin)
           || User.IsInRole(AppRoles.Principal)
           || User.IsInRole(AppRoles.Admin);

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is null || string.IsNullOrWhiteSpace(user.Id))
            return null;

        return (user.SchoolId.Value, user.Id);
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school before using attendance.";
        return RedirectToAction("Index", "SchoolSetup");
    }
}

