using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using School_Management_System.Models;

namespace School_Management_System.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() switch
        {
            AppRoles.SuperAdmin => RedirectToAction(nameof(SuperAdmin)),
            AppRoles.Principal => RedirectToAction(nameof(Principal)),
            AppRoles.Admin => RedirectToAction(nameof(Admin)),
            AppRoles.Accountant => RedirectToAction(nameof(Accountant)),
            AppRoles.Teacher => RedirectToAction(nameof(Teacher)),
            AppRoles.ExamController => RedirectToAction(nameof(ExamController)),
            AppRoles.HR => RedirectToAction(nameof(HR)),
            AppRoles.Receptionist => RedirectToAction(nameof(Receptionist)),
            AppRoles.Parent => RedirectToAction(nameof(Parent)),
            AppRoles.Student => RedirectToAction(nameof(Student)),
            _ => View("RoleDashboard", model: "User")
        };
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    public IActionResult SuperAdmin() => RoleView("Super Admin / Vendor");

    [Authorize(Roles = AppRoles.Principal)]
    public IActionResult Principal() => RoleView("School Owner / Principal");

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Admin() => RoleView("Administrator");

    [Authorize(Roles = AppRoles.Accountant)]
    public IActionResult Accountant() => RoleView("Accountant");

    [Authorize(Roles = AppRoles.Teacher)]
    public IActionResult Teacher() => RoleView("Teacher / Class Teacher");

    [Authorize(Roles = AppRoles.ExamController)]
    public IActionResult ExamController() => RoleView("Exam Controller");

    [Authorize(Roles = AppRoles.HR)]
    public IActionResult HR() => RoleView("HR / Payroll Officer");

    [Authorize(Roles = AppRoles.Receptionist)]
    public IActionResult Receptionist() => RoleView("Reception / Admission Officer");

    [Authorize(Roles = AppRoles.Parent)]
    public IActionResult Parent() => RoleView("Parent");

    [Authorize(Roles = AppRoles.Student)]
    public IActionResult Student() => RoleView("Student");

    private IActionResult RoleView(string roleName)
    {
        ViewData["RoleName"] = roleName;
        return View("RoleDashboard");
    }
}
