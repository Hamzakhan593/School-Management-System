using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
public class AcademicSessionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IAcademicSessionService _sessionService;
    private readonly IAuditService _audit;

    public AcademicSessionsController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IAcademicSessionService sessionService,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _sessionService = sessionService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var sessions = await _db.AcademicSessions
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId.Value)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        return View(sessions);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        return View(new AcademicSessionFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AcademicSessionFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        ValidateSessionDates(model);
        if (await _db.AcademicSessions.AnyAsync(x => x.SchoolId == schoolId.Value && x.Name == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "A session with this name already exists.");

        if (!ModelState.IsValid) return View(model);

        var session = new AcademicSession
        {
            SchoolId = schoolId.Value,
            Name = model.Name.Trim(),
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            WorkingDaysPerWeek = model.WorkingDaysPerWeek,
            Notes = model.Notes?.Trim(),
            Status = AcademicSessionStatus.Draft
        };

        _db.AcademicSessions.Add(session);
        await _db.SaveChangesAsync();

        _db.GradingSchemes.Add(new GradingScheme
        {
            AcademicSessionId = session.Id,
            Name = "Default Grading Scheme",
            IsDefault = true
        });
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("AcademicSession.Created", "AcademicSession", session.Id.ToString(), session.Name);
        TempData["Success"] = "Academic session created as Draft.";
        return RedirectToAction(nameof(Setup), new { id = session.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var session = await FindOwnedSessionAsync(id);
        if (session is null) return NotFound();
        if (!CanEditSession(session)) return Forbid();

        return View(new AcademicSessionFormViewModel
        {
            Id = session.Id,
            Name = session.Name,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            WorkingDaysPerWeek = session.WorkingDaysPerWeek,
            Notes = session.Notes,
            CurrentStatus = session.Status.ToString()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AcademicSessionFormViewModel model)
    {
        var session = await FindOwnedSessionAsync(model.Id);
        if (session is null) return NotFound();
        if (!CanEditSession(session)) return Forbid();

        ValidateSessionDates(model);
        if (await _db.AcademicSessions.AnyAsync(x => x.SchoolId == session.SchoolId && x.Name == model.Name.Trim() && x.Id != session.Id))
            ModelState.AddModelError(nameof(model.Name), "A session with this name already exists.");

        if (!ModelState.IsValid)
        {
            model.CurrentStatus = session.Status.ToString();
            return View(model);
        }

        session.Name = model.Name.Trim();
        session.StartDate = model.StartDate.Date;
        session.EndDate = model.EndDate.Date;
        session.WorkingDaysPerWeek = model.WorkingDaysPerWeek;
        session.Notes = model.Notes?.Trim();
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicSession.Updated", "AcademicSession", session.Id.ToString(), $"{session.Name}; Status={session.Status}");

        TempData["Success"] = "Academic session updated.";
        return RedirectToAction(nameof(Setup), new { id = session.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Setup(int id)
    {
        var session = await _db.AcademicSessions
            .AsNoTracking()
            .Include(x => x.Terms.OrderBy(t => t.DisplayOrder).ThenBy(t => t.StartDate))
            .Include(x => x.Holidays.OrderBy(h => h.StartDate))
            .Include(x => x.GradingSchemes).ThenInclude(x => x.Rules.OrderByDescending(r => r.MinPercentage))
            .FirstOrDefaultAsync(x => x.Id == id);

        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        if (session is null || session.SchoolId != schoolId.Value) return NotFound();

        var model = new AcademicSessionSetupViewModel
        {
            Session = session,
            NewTerm = new TermCreateViewModel { AcademicSessionId = id, StartDate = session.StartDate, EndDate = session.EndDate },
            NewHoliday = new HolidayCreateViewModel { AcademicSessionId = id, StartDate = session.StartDate, EndDate = session.StartDate },
            NewGradeRule = new GradeRuleCreateViewModel { AcademicSessionId = id, MinPercentage = 0, MaxPercentage = 100 }
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id) => await RunTransitionAsync(id, _sessionService.ActivateAsync);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id) => await RunTransitionAsync(id, _sessionService.CloseAsync);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal)]
    public async Task<IActionResult> Archive(int id) => await RunTransitionAsync(id, _sessionService.ArchiveAsync);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTerm(TermCreateViewModel model)
    {
        var session = await FindOwnedSessionAsync(model.AcademicSessionId);
        if (session is null) return NotFound();
        if (!CanEditSetup(session)) return Forbid();

        if (model.EndDate.Date < model.StartDate.Date)
            ModelState.AddModelError(string.Empty, "Term end date cannot be before start date.");
        if (model.StartDate.Date < session.StartDate.Date || model.EndDate.Date > session.EndDate.Date)
            ModelState.AddModelError(string.Empty, "Term dates must remain inside the academic session dates.");
        if (await _db.Terms.AnyAsync(x => x.AcademicSessionId == session.Id && x.Name == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "A term with this name already exists.");

        if (!ModelState.IsValid) return SetupValidationRedirect(session.Id, ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage);

        _db.Terms.Add(new Term
        {
            AcademicSessionId = session.Id,
            Name = model.Name.Trim(),
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            DisplayOrder = model.DisplayOrder
        });
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("Term.Created", "AcademicSession", session.Id.ToString(), model.Name.Trim());
        TempData["Success"] = "Term added.";
        return RedirectToAction(nameof(Setup), new { id = session.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTerm(int id)
    {
        var term = await _db.Terms.Include(x => x.AcademicSession).FirstOrDefaultAsync(x => x.Id == id);
        if (term is null) return NotFound();
        if (!await OwnsSchoolAsync(term.AcademicSession.SchoolId)) return NotFound();
        if (!CanEditSetup(term.AcademicSession)) return Forbid();

        _db.Terms.Remove(term);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("Term.Deleted", "Term", id.ToString(), term.Name);
        TempData["Success"] = "Term removed.";
        return RedirectToAction(nameof(Setup), new { id = term.AcademicSessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHoliday(HolidayCreateViewModel model)
    {
        var session = await FindOwnedSessionAsync(model.AcademicSessionId);
        if (session is null) return NotFound();
        if (!CanEditSetup(session)) return Forbid();

        if (model.EndDate.Date < model.StartDate.Date)
            ModelState.AddModelError(string.Empty, "Holiday end date cannot be before start date.");
        if (model.StartDate.Date < session.StartDate.Date || model.EndDate.Date > session.EndDate.Date)
            ModelState.AddModelError(string.Empty, "Holiday dates must remain inside the academic session dates.");

        if (!ModelState.IsValid) return SetupValidationRedirect(session.Id, ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage);

        _db.SchoolHolidays.Add(new SchoolHoliday
        {
            AcademicSessionId = session.Id,
            Name = model.Name.Trim(),
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            Notes = model.Notes?.Trim()
        });
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("Holiday.Created", "AcademicSession", session.Id.ToString(), model.Name.Trim());
        TempData["Success"] = "Holiday/calendar entry added.";
        return RedirectToAction(nameof(Setup), new { id = session.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var holiday = await _db.SchoolHolidays.Include(x => x.AcademicSession).FirstOrDefaultAsync(x => x.Id == id);
        if (holiday is null) return NotFound();
        if (!await OwnsSchoolAsync(holiday.AcademicSession.SchoolId)) return NotFound();
        if (!CanEditSetup(holiday.AcademicSession)) return Forbid();

        _db.SchoolHolidays.Remove(holiday);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("Holiday.Deleted", "SchoolHoliday", id.ToString(), holiday.Name);
        TempData["Success"] = "Holiday/calendar entry removed.";
        return RedirectToAction(nameof(Setup), new { id = holiday.AcademicSessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGradeRule(GradeRuleCreateViewModel model)
    {
        var session = await FindOwnedSessionAsync(model.AcademicSessionId);
        if (session is null) return NotFound();
        if (!CanEditSetup(session)) return Forbid();

        if (model.MaxPercentage < model.MinPercentage)
            ModelState.AddModelError(string.Empty, "Maximum percentage must be greater than or equal to minimum percentage.");

        var scheme = await _db.GradingSchemes.Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.AcademicSessionId == session.Id && x.IsDefault);
        if (scheme is null)
        {
            scheme = new GradingScheme { AcademicSessionId = session.Id, Name = "Default Grading Scheme", IsDefault = true };
            _db.GradingSchemes.Add(scheme);
            await _db.SaveChangesAsync();
        }

        if (scheme.Rules.Any(x => string.Equals(x.Grade, model.Grade.Trim(), StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(model.Grade), "This grade already exists.");

        if (scheme.Rules.Any(x => model.MinPercentage <= x.MaxPercentage && model.MaxPercentage >= x.MinPercentage))
            ModelState.AddModelError(string.Empty, "This percentage range overlaps an existing grade rule.");

        if (!ModelState.IsValid) return SetupValidationRedirect(session.Id, ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage);

        _db.GradingRules.Add(new GradingRule
        {
            GradingSchemeId = scheme.Id,
            Grade = model.Grade.Trim(),
            MinPercentage = model.MinPercentage,
            MaxPercentage = model.MaxPercentage,
            Remarks = model.Remarks?.Trim()
        });
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("GradingRule.Created", "AcademicSession", session.Id.ToString(), $"{model.Grade}: {model.MinPercentage}-{model.MaxPercentage}");
        TempData["Success"] = "Grading rule added.";
        return RedirectToAction(nameof(Setup), new { id = session.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGradeRule(int id)
    {
        var rule = await _db.GradingRules
            .Include(x => x.GradingScheme).ThenInclude(x => x.AcademicSession)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (rule is null) return NotFound();
        if (!await OwnsSchoolAsync(rule.GradingScheme.AcademicSession.SchoolId)) return NotFound();
        if (!CanEditSetup(rule.GradingScheme.AcademicSession)) return Forbid();

        var sessionId = rule.GradingScheme.AcademicSessionId;
        _db.GradingRules.Remove(rule);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("GradingRule.Deleted", "GradingRule", id.ToString(), rule.Grade);
        TempData["Success"] = "Grading rule removed.";
        return RedirectToAction(nameof(Setup), new { id = sessionId });
    }

    private async Task<IActionResult> RunTransitionAsync(int id, Func<int, int, Task<(bool Success, string Message)>> action)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        var result = await action(id, schoolId.Value);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<AcademicSession?> FindOwnedSessionAsync(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return null;
        return await _db.AcademicSessions.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value);
    }

    private bool CanEditSession(AcademicSession session)
    {
        if (session.Status is AcademicSessionStatus.Draft or AcademicSessionStatus.Active) return true;
        return User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal);
    }

    private bool CanEditSetup(AcademicSession session)
    {
        if (session.Status is AcademicSessionStatus.Draft or AcademicSessionStatus.Active) return true;
        return User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal);
    }

    private async Task<bool> OwnsSchoolAsync(int schoolId)
    {
        var currentSchoolId = await RequireSchoolIdAsync();
        return currentSchoolId == schoolId;
    }

    private async Task<int?> RequireSchoolIdAsync() => await _schoolContext.GetCurrentSchoolIdAsync();

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Info"] = "Create the School Profile first, then configure academic sessions.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private IActionResult SetupValidationRedirect(int sessionId, string? message)
    {
        TempData["Error"] = string.IsNullOrWhiteSpace(message) ? "Please check the entered values." : message;
        return RedirectToAction(nameof(Setup), new { id = sessionId });
    }

    private void ValidateSessionDates(AcademicSessionFormViewModel model)
    {
        if (model.EndDate.Date <= model.StartDate.Date)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");
    }
}
