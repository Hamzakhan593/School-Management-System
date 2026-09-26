using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Accountant + "," + AppRoles.Teacher + "," + AppRoles.ExamController + "," + AppRoles.HR + "," + AppRoles.Receptionist)]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext db, ISchoolContextService schoolContext, IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _schoolContext = schoolContext;
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    public IActionResult Index() => View();

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> Executive(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = await _dashboardService.BuildAsync(user, AppRoles.Principal, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Receptionist)]
    public async Task<IActionResult> StudentRegister(int? schoolClassId, string? search, CancellationToken cancellationToken)
    {
        var school = await _schoolContext.GetCurrentSchoolAsync();
        if (school is null) return RedirectToAction("Index", "SchoolSetup");

        var session = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == school.Id && x.Status == AcademicSessionStatus.Active)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var query = _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == school.Id && x.IsCurrent && x.Status == StudentEnrollmentStatus.Active && x.Student.Status == StudentStatus.Active);
        if (session is not null) query = query.Where(x => x.AcademicSessionId == session.Id);
        if (schoolClassId.HasValue) query = query.Where(x => x.SchoolClassId == schoolClassId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Student.FullName.Contains(term) || x.Student.AdmissionNumber.Contains(term) || (x.RollNumber != null && x.RollNumber.Contains(term)));
        }

        var rows = await query
            .OrderBy(x => x.ClassName).ThenBy(x => x.SectionName).ThenBy(x => x.RollNumber).ThenBy(x => x.Student.FullName)
            .Select(x => new StudentRegisterRow
            {
                StudentId = x.StudentId,
                AdmissionNumber = x.Student.AdmissionNumber,
                RollNumber = x.RollNumber,
                StudentName = x.Student.FullName,
                GuardianName = x.Student.FatherGuardianName ?? "-",
                ClassName = x.ClassName,
                SectionName = x.SectionName ?? "-",
                ContactNumber = x.Student.ContactNumber,
                Status = "Active"
            })
            .ToListAsync(cancellationToken);

        var model = new StudentRegisterReportViewModel
        {
            SchoolName = school.Name,
            SessionName = session?.Name ?? "No active session",
            SchoolClassId = schoolClassId,
            Search = search,
            Rows = rows,
            Classes = await _db.SchoolClasses.AsNoTracking()
                .Where(x => x.SchoolId == school.Id && x.IsActive)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new ReportOption { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Receptionist)]
    public async Task<IActionResult> StudentRegisterCsv(int? schoolClassId, string? search, CancellationToken cancellationToken)
    {
        var result = await StudentRegister(schoolClassId, search, cancellationToken) as ViewResult;
        if (result?.Model is not StudentRegisterReportViewModel model) return RedirectToAction(nameof(StudentRegister));

        var sb = new StringBuilder();
        sb.AppendLine("Admission No,Roll No,Student,Guardian,Class,Section,Contact,Status");
        foreach (var row in model.Rows)
        {
            sb.AppendLine(string.Join(",", Csv(row.AdmissionNumber), Csv(row.RollNumber), Csv(row.StudentName), Csv(row.GuardianName), Csv(row.ClassName), Csv(row.SectionName), Csv(row.ContactNumber), Csv(row.Status)));
        }
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv", $"student-register-{DateTime.Today:yyyyMMdd}.csv");
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> AuditActivity(string? search, string? actionName, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value.Date);
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(actionName)) query = query.Where(x => x.Action.Contains(actionName.Trim()));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => (x.UserEmail != null && x.UserEmail.Contains(term)) || x.EntityType.Contains(term) || (x.Details != null && x.Details.Contains(term)));
        }

        var model = new AuditActivityReportViewModel
        {
            Search = search,
            ActionName = actionName,
            From = from,
            To = to,
            Rows = await query.OrderByDescending(x => x.CreatedAtUtc).Take(500)
                .Select(x => new AuditActivityRow
                {
                    Id = x.Id,
                    Date = x.CreatedAtUtc,
                    User = x.UserEmail ?? "System",
                    Action = x.Action,
                    Entity = x.EntityType,
                    EntityId = x.EntityId,
                    Details = x.Details,
                    IpAddress = x.IpAddress
                }).ToListAsync(cancellationToken)
        };
        return View(model);
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
