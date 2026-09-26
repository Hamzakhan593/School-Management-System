using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDashboardService _dashboardService;

    public DashboardController(UserManager<ApplicationUser> userManager, IDashboardService dashboardService)
    {
        _userManager = userManager;
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var roles = await _userManager.GetRolesAsync(user);
        var role = GetPrimaryRole(roles);
        var model = await _dashboardService.BuildAsync(user, role, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    public Task<IActionResult> SuperAdmin(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.SuperAdmin, cancellationToken);

    [Authorize(Roles = AppRoles.Principal)]
    public Task<IActionResult> Principal(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.Principal, cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    public Task<IActionResult> Admin(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.Admin, cancellationToken);

    [Authorize(Roles = AppRoles.Accountant)]
    public Task<IActionResult> Accountant(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.Accountant, cancellationToken);

    [Authorize(Roles = AppRoles.Teacher)]
    public Task<IActionResult> Teacher(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.Teacher, cancellationToken);

    [Authorize(Roles = AppRoles.ExamController)]
    public Task<IActionResult> ExamController(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.ExamController, cancellationToken);

    [Authorize(Roles = AppRoles.HR)]
    public Task<IActionResult> HR(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.HR, cancellationToken);

    [Authorize(Roles = AppRoles.Receptionist)]
    public Task<IActionResult> Receptionist(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.Receptionist, cancellationToken);

    [Authorize(Roles = AppRoles.Parent)]
    public Task<IActionResult> Parent(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.Parent, cancellationToken);

    [Authorize(Roles = AppRoles.Student)]
    public Task<IActionResult> Student(CancellationToken cancellationToken) => RenderRoleAsync(AppRoles.Student, cancellationToken);

    private async Task<IActionResult> RenderRoleAsync(string role, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = await _dashboardService.BuildAsync(user, role, cancellationToken);
        return View("Index", model);
    }

    private static string GetPrimaryRole(IList<string> roles)
    {
        foreach (var role in AppRoles.All)
        {
            if (roles.Contains(role, StringComparer.OrdinalIgnoreCase)) return role;
        }
        return roles.FirstOrDefault() ?? "User";
    }
}
