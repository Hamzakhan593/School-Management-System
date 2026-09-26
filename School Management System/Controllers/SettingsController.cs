using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly ISystemSettingsService _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditService _audit;

    public SettingsController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        ISystemSettingsService settings,
        IWebHostEnvironment environment,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _settings = settings;
        _environment = environment;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is not int schoolId)
        {
            TempData["Error"] = "Complete the School Profile before opening Settings.";
            return RedirectToAction("Index", "SchoolSetup");
        }

        return View(await BuildViewModelAsync(schoolId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Index(SettingsPageViewModel model, CancellationToken cancellationToken)
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is not int schoolId)
            return RedirectToAction("Index", "SchoolSetup");

        ValidatePrefix(model.AdmissionNumberPrefix, nameof(model.AdmissionNumberPrefix));
        ValidatePrefix(model.ChallanNumberPrefix, nameof(model.ChallanNumberPrefix));
        ValidatePrefix(model.ReceiptNumberPrefix, nameof(model.ReceiptNumberPrefix));
        ValidateSignature(model.PrincipalSignatureFile, nameof(model.PrincipalSignatureFile));
        ValidateSignature(model.ClassTeacherSignatureFile, nameof(model.ClassTeacherSignatureFile));

        if (!ModelState.IsValid)
        {
            await PopulateDisplayFieldsAsync(model, schoolId, cancellationToken);
            return View(model);
        }

        var entity = await _settings.GetTrackedAsync(schoolId, cancellationToken);
        var oldSummary = JsonSerializer.Serialize(new
        {
            entity.AdmissionNumberPrefix,
            entity.ChallanNumberPrefix,
            entity.ReceiptNumberPrefix,
            entity.DefaultFeeDueDay,
            entity.LowAttendanceThresholdPercent,
            entity.BackupRetentionDays,
            entity.SessionTimeoutMinutes
        });

        entity.AdmissionNumberPrefix = NormalizePrefix(model.AdmissionNumberPrefix);
        entity.AdmissionNumberDigits = model.AdmissionNumberDigits;
        entity.ChallanNumberPrefix = NormalizePrefix(model.ChallanNumberPrefix);
        entity.ReceiptNumberPrefix = NormalizePrefix(model.ReceiptNumberPrefix);
        entity.FinancialNumberDigits = model.FinancialNumberDigits;
        entity.FiscalYearStartMonth = model.FiscalYearStartMonth;

        entity.DefaultFeeDueDay = model.DefaultFeeDueDay;
        entity.LateFeeFixedAmount = decimal.Round(model.LateFeeFixedAmount, 2);
        entity.LateFeeGraceDays = model.LateFeeGraceDays;
        entity.ApplyLateFeeOnCollection = model.ApplyLateFeeOnCollection;
        entity.AutoGenerateMonthlyChallans = model.AutoGenerateMonthlyChallans;
        entity.MonthlyChallanGenerationDay = model.MonthlyChallanGenerationDay;

        entity.TeacherAttendanceEditCutoffHours = model.TeacherAttendanceEditCutoffHours;
        entity.LowAttendanceThresholdPercent = decimal.Round(model.LowAttendanceThresholdPercent, 2);
        entity.AttendanceAllowLate = model.AttendanceAllowLate;
        entity.AttendanceAllowLeave = model.AttendanceAllowLeave;
        entity.AttendanceAllowHalfDay = model.AttendanceAllowHalfDay;
        entity.AttendanceAllowNoClass = model.AttendanceAllowNoClass;

        entity.ResultCardShowAttendance = model.ResultCardShowAttendance;
        entity.ResultCardShowClassPosition = model.ResultCardShowClassPosition;
        entity.ClassTeacherSignatureLabel = Clean(model.ClassTeacherSignatureLabel) ?? "Class Teacher Signature";
        entity.PrincipalSignatureLabel = Clean(model.PrincipalSignatureLabel) ?? "Principal Signature";
        entity.ResultCardFooterText = Clean(model.ResultCardFooterText);
        entity.PayslipFooterText = Clean(model.PayslipFooterText);
        entity.GeneralPrintFooterText = Clean(model.GeneralPrintFooterText);

        if (model.PrincipalSignatureFile is not null)
            entity.PrincipalSignaturePath = await SaveSignatureAsync(model.PrincipalSignatureFile, "principal", cancellationToken);
        if (model.ClassTeacherSignatureFile is not null)
            entity.ClassTeacherSignaturePath = await SaveSignatureAsync(model.ClassTeacherSignatureFile, "class-teacher", cancellationToken);

        entity.ScheduledBackupsEnabled = model.ScheduledBackupsEnabled;
        entity.BackupHourLocal = model.BackupHourLocal;
        entity.BackupRetentionDays = model.BackupRetentionDays;
        entity.AllowInAppRestore = model.AllowInAppRestore;

        entity.SessionTimeoutMinutes = model.SessionTimeoutMinutes;
        entity.PasswordRequiredLength = model.PasswordRequiredLength;
        entity.PasswordRequireDigit = model.PasswordRequireDigit;
        entity.PasswordRequireUppercase = model.PasswordRequireUppercase;
        entity.PasswordRequireLowercase = model.PasswordRequireLowercase;
        entity.PasswordRequireSpecialCharacter = model.PasswordRequireSpecialCharacter;

        entity.EmailNotificationsEnabled = model.EmailNotificationsEnabled;
        entity.SmsNotificationsEnabled = model.SmsNotificationsEnabled;
        entity.WhatsAppNotificationsEnabled = model.WhatsAppNotificationsEnabled;
        entity.EmailProviderName = Clean(model.EmailProviderName);
        entity.SmsProviderName = Clean(model.SmsProviderName);
        entity.WhatsAppProviderName = Clean(model.WhatsAppProviderName);

        entity.EnableBiometricAttendance = model.EnableBiometricAttendance;
        entity.EnableCameraAttendance = model.EnableCameraAttendance;
        entity.EnableParentStudentPortal = model.EnableParentStudentPortal;
        entity.EnableOnlinePayments = model.EnableOnlinePayments;
        entity.EnablePushNotifications = model.EnablePushNotifications;
        entity.SchoolTimeZoneId = Clean(model.SchoolTimeZoneId) ?? "Asia/Karachi";
        entity.UpdatedByUserId = user.Id;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Settings were changed by another user. Reload the page and try again.");
            await PopulateDisplayFieldsAsync(model, schoolId, cancellationToken);
            return View(model);
        }

        var newSummary = JsonSerializer.Serialize(new
        {
            entity.AdmissionNumberPrefix,
            entity.ChallanNumberPrefix,
            entity.ReceiptNumberPrefix,
            entity.DefaultFeeDueDay,
            entity.LowAttendanceThresholdPercent,
            entity.BackupRetentionDays,
            entity.SessionTimeoutMinutes
        });
        await _audit.WriteAsync("Settings.Updated", nameof(SystemSetting), entity.Id.ToString(), "School master settings updated.", oldSummary, newSummary);

        TempData["Success"] = "Settings saved. New admissions, fee numbering, attendance rules, backup scheduling and security policy now use the updated values.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSignature(string type, CancellationToken cancellationToken)
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is not int schoolId) return RedirectToAction("Index", "SchoolSetup");
        var entity = await _settings.GetTrackedAsync(schoolId, cancellationToken);

        if (string.Equals(type, "principal", StringComparison.OrdinalIgnoreCase))
            entity.PrincipalSignaturePath = null;
        else if (string.Equals(type, "class-teacher", StringComparison.OrdinalIgnoreCase))
            entity.ClassTeacherSignaturePath = null;
        else
            return BadRequest();

        entity.UpdatedByUserId = user.Id;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Settings.SignatureRemoved", nameof(SystemSetting), entity.Id.ToString(), type);
        TempData["Success"] = "Signature image removed from print settings.";
        return RedirectToAction(nameof(Index), new { tab = "print" });
    }

    private async Task<SettingsPageViewModel> BuildViewModelAsync(int schoolId, CancellationToken cancellationToken)
    {
        var s = await _settings.GetAsync(schoolId, cancellationToken);
        var model = new SettingsPageViewModel
        {
            Id = s.Id,
            AdmissionNumberPrefix = s.AdmissionNumberPrefix,
            AdmissionNumberDigits = s.AdmissionNumberDigits,
            ChallanNumberPrefix = s.ChallanNumberPrefix,
            ReceiptNumberPrefix = s.ReceiptNumberPrefix,
            FinancialNumberDigits = s.FinancialNumberDigits,
            FiscalYearStartMonth = s.FiscalYearStartMonth,
            DefaultFeeDueDay = s.DefaultFeeDueDay,
            LateFeeFixedAmount = s.LateFeeFixedAmount,
            LateFeeGraceDays = s.LateFeeGraceDays,
            ApplyLateFeeOnCollection = s.ApplyLateFeeOnCollection,
            AutoGenerateMonthlyChallans = s.AutoGenerateMonthlyChallans,
            MonthlyChallanGenerationDay = s.MonthlyChallanGenerationDay,
            TeacherAttendanceEditCutoffHours = s.TeacherAttendanceEditCutoffHours,
            LowAttendanceThresholdPercent = s.LowAttendanceThresholdPercent,
            AttendanceAllowLate = s.AttendanceAllowLate,
            AttendanceAllowLeave = s.AttendanceAllowLeave,
            AttendanceAllowHalfDay = s.AttendanceAllowHalfDay,
            AttendanceAllowNoClass = s.AttendanceAllowNoClass,
            ResultCardShowAttendance = s.ResultCardShowAttendance,
            ResultCardShowClassPosition = s.ResultCardShowClassPosition,
            ClassTeacherSignatureLabel = s.ClassTeacherSignatureLabel,
            PrincipalSignatureLabel = s.PrincipalSignatureLabel,
            ResultCardFooterText = s.ResultCardFooterText,
            PayslipFooterText = s.PayslipFooterText,
            GeneralPrintFooterText = s.GeneralPrintFooterText,
            PrincipalSignaturePath = s.PrincipalSignaturePath,
            ClassTeacherSignaturePath = s.ClassTeacherSignaturePath,
            ScheduledBackupsEnabled = s.ScheduledBackupsEnabled,
            BackupHourLocal = s.BackupHourLocal,
            BackupRetentionDays = s.BackupRetentionDays,
            AllowInAppRestore = s.AllowInAppRestore,
            SessionTimeoutMinutes = s.SessionTimeoutMinutes,
            PasswordRequiredLength = s.PasswordRequiredLength,
            PasswordRequireDigit = s.PasswordRequireDigit,
            PasswordRequireUppercase = s.PasswordRequireUppercase,
            PasswordRequireLowercase = s.PasswordRequireLowercase,
            PasswordRequireSpecialCharacter = s.PasswordRequireSpecialCharacter,
            EmailNotificationsEnabled = s.EmailNotificationsEnabled,
            SmsNotificationsEnabled = s.SmsNotificationsEnabled,
            WhatsAppNotificationsEnabled = s.WhatsAppNotificationsEnabled,
            EmailProviderName = s.EmailProviderName,
            SmsProviderName = s.SmsProviderName,
            WhatsAppProviderName = s.WhatsAppProviderName,
            EnableBiometricAttendance = s.EnableBiometricAttendance,
            EnableCameraAttendance = s.EnableCameraAttendance,
            EnableParentStudentPortal = s.EnableParentStudentPortal,
            EnableOnlinePayments = s.EnableOnlinePayments,
            EnablePushNotifications = s.EnablePushNotifications,
            SchoolTimeZoneId = s.SchoolTimeZoneId,
            LastUpdatedAtUtc = s.UpdatedAtUtc
        };
        await PopulateDisplayFieldsAsync(model, schoolId, cancellationToken);
        return model;
    }

    private async Task PopulateDisplayFieldsAsync(SettingsPageViewModel model, int schoolId, CancellationToken cancellationToken)
    {
        var school = await _db.Schools.AsNoTracking().FirstAsync(x => x.Id == schoolId, cancellationToken);
        model.SchoolName = school.Name;
        model.SchoolLogoPath = school.LogoPath;
        var activeSession = await _db.AcademicSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active, cancellationToken);
        model.ActiveSessionName = activeSession?.Name;
        model.FeeHeadCount = await _db.FeeHeads.AsNoTracking().CountAsync(x => x.SchoolId == schoolId && x.IsActive, cancellationToken);
        model.GradingRuleCount = activeSession is null ? 0 : await _db.GradingRules.AsNoTracking().CountAsync(x => x.GradingScheme.AcademicSessionId == activeSession.Id, cancellationToken);
        if (model.Id > 0)
        {
            model.LastUpdatedBy = await _db.SystemSettings.AsNoTracking()
                .Where(x => x.Id == model.Id)
                .Select(x => x.UpdatedByUser != null ? x.UpdatedByUser.FullName : null)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private void ValidatePrefix(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Any(c => !char.IsLetterOrDigit(c) && c != '-'))
            ModelState.AddModelError(key, "Use only letters, numbers and hyphens in numbering prefixes.");
    }

    private void ValidateSignature(IFormFile? file, string key)
    {
        if (file is null) return;
        if (file.Length > 2 * 1024 * 1024)
            ModelState.AddModelError(key, "Signature image must be 2 MB or smaller.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".webp")
            ModelState.AddModelError(key, "Signature image must be PNG, JPG, JPEG or WEBP.");
    }

    private async Task<string> SaveSignatureAsync(IFormFile file, string label, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(_environment.WebRootPath, "uploads", "settings");
        Directory.CreateDirectory(folder);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var name = $"{label}-{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, name);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);
        return "/uploads/settings/" + name;
    }

    private static string NormalizePrefix(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
