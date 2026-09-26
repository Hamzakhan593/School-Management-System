using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.UserManagers)]
public class BackupController : Controller
{
    private readonly IBackupService _backupService;
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public BackupController(IBackupService backupService, ApplicationDbContext db, ISchoolContextService schoolContext,
        UserManager<ApplicationUser> userManager)
    {
        _backupService = backupService;
        _db = db;
        _schoolContext = schoolContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _backupService.GetDashboardAsync(cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? notes, CancellationToken cancellationToken)
    {
        try
        {
            var backup = await _backupService.CreateBackupAsync(BackupType.Manual, notes, cancellationToken);
            TempData["Success"] = $"Backup created successfully: {backup.FileName}";
        }
        catch (Exception ex) { TempData["Error"] = $"Backup failed: {ex.Message}"; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(long id, CancellationToken cancellationToken)
    {
        try
        {
            await _backupService.VerifyAsync(id, cancellationToken);
            TempData["Success"] = "Backup integrity verification passed.";
        }
        catch (Exception ex) { TempData["Error"] = $"Verification failed: {ex.Message}"; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        try
        {
            await _backupService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Backup file was deleted and the history record was retained.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal)]
    [HttpGet]
    public async Task<IActionResult> Restore(long id, CancellationToken cancellationToken)
    {
        var dashboard = await _backupService.GetDashboardAsync(cancellationToken);
        var backup = dashboard.Backups.FirstOrDefault(x => x.Id == id && x.Status == BackupStatus.Succeeded);
        if (backup is null) return NotFound();
        return View(new RestoreBackupViewModel
        {
            BackupId = backup.Id,
            FileName = backup.FileName,
            SizeBytes = backup.SizeBytes,
            CompletedAtUtc = backup.CompletedAtUtc,
            IsVerified = backup.IsVerified
        });
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(RestoreBackupViewModel model, CancellationToken cancellationToken)
    {
        if (!string.Equals(model.ConfirmationText?.Trim(), "RESTORE", StringComparison.Ordinal))
            ModelState.AddModelError(nameof(model.ConfirmationText), "Type RESTORE exactly to confirm.");

        var user = await _userManager.GetUserAsync(User);
        if (user is null || string.IsNullOrWhiteSpace(model.CurrentPassword) || !await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
            ModelState.AddModelError(nameof(model.CurrentPassword), "Password confirmation failed.");

        if (!ModelState.IsValid)
        {
            var dashboard = await _backupService.GetDashboardAsync(cancellationToken);
            var backup = dashboard.Backups.FirstOrDefault(x => x.Id == model.BackupId);
            if (backup is not null)
            {
                model.FileName = backup.FileName;
                model.SizeBytes = backup.SizeBytes;
                model.CompletedAtUtc = backup.CompletedAtUtc;
                model.IsVerified = backup.IsVerified;
            }
            return View(model);
        }

        try
        {
            await _backupService.RestoreAsync(model.BackupId, model.Reason, cancellationToken);
            TempData["Success"] = "Database restore completed. A safety backup was created before restore.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Restore failed: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> AuditLogs(string? search, string? action, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(x => x.SchoolId == schoolId || x.SchoolId == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => (x.UserEmail ?? "").Contains(term) || (x.EntityType ?? "").Contains(term) ||
                (x.EntityId ?? "").Contains(term) || (x.Details ?? "").Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action.Contains(action.Trim()));
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value.Date.ToUniversalTime());
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < to.Value.Date.AddDays(1).ToUniversalTime());

        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(cancellationToken);
        return View(new AuditLogIndexViewModel { Items = items, Search = search, Action = action, From = from, To = to });
    }
}
