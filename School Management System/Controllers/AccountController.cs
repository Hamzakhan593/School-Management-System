using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuditService audit,
        IWebHostEnvironment environment,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _audit = audit;
        _environment = environment;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account is inactive. Contact the school administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await SafeAuditAsync("Auth.Login.Success", "ApplicationUser", user.Id);

            if (user.ForcePasswordChange)
            {
                TempData["Info"] = "Please change your temporary password before continuing.";
                return RedirectToAction(nameof(ChangePassword));
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return await RedirectToRoleDashboardAsync(user);
        }

        if (result.IsLockedOut)
        {
            await SafeAuditAsync("Auth.Login.LockedOut", "ApplicationUser", user.Id);
            ModelState.AddModelError(string.Empty, "Account locked after repeated failed attempts. Try again later or contact an administrator.");
            return View(model);
        }

        await SafeAuditAsync("Auth.Login.Failed", "ApplicationUser", user.Id);
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var id = _userManager.GetUserId(User);
        await SafeAuditAsync("Auth.Logout", "ApplicationUser", id);
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action(nameof(ResetPassword), "Account",
                new { email = user.Email, code = token }, Request.Scheme);

            _logger.LogInformation("Password reset link for {Email}: {ResetUrl}", user.Email, resetUrl);

            if (_environment.IsDevelopment())
            {
                TempData["DeveloperResetLink"] = resetUrl;
            }

            await SafeAuditAsync("Auth.PasswordReset.Requested", "ApplicationUser", user.Id);
        }

        // Deliberately do not reveal whether the email exists.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("A valid reset link is required.");
        }

        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded)
        {
            user.ForcePasswordChange = false;
            await _userManager.UpdateAsync(user);
            await SafeAuditAsync("Auth.PasswordReset.Completed", "ApplicationUser", user.Id);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.ForcePasswordChange = false;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        await SafeAuditAsync("Auth.Password.Changed", "ApplicationUser", user.Id);

        TempData["Success"] = "Password changed successfully.";
        return await RedirectToRoleDashboardAsync(user);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task<IActionResult> RedirectToRoleDashboardAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        return role switch
        {
            AppRoles.SuperAdmin => RedirectToAction("SuperAdmin", "Dashboard"),
            AppRoles.Principal => RedirectToAction("Principal", "Dashboard"),
            AppRoles.Admin => RedirectToAction("Admin", "Dashboard"),
            AppRoles.Accountant => RedirectToAction("Accountant", "Dashboard"),
            AppRoles.Teacher => RedirectToAction("Teacher", "Dashboard"),
            AppRoles.ExamController => RedirectToAction("ExamController", "Dashboard"),
            AppRoles.HR => RedirectToAction("HR", "Dashboard"),
            AppRoles.Receptionist => RedirectToAction("Receptionist", "Dashboard"),
            AppRoles.Parent => RedirectToAction("Parent", "Dashboard"),
            AppRoles.Student => RedirectToAction("Student", "Dashboard"),
            _ => RedirectToAction("Index", "Dashboard")
        };
    }

    private async Task SafeAuditAsync(string action, string entityType = "", string? entityId = null, string? details = null)
    {
        try
        {
            await _audit.WriteAsync(action, entityType, entityId, details);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to write audit event {Action}", action);
        }
    }
}
