using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.UserManagers)]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public UserManagementController(UserManager<ApplicationUser> userManager, IAuditService audit)
    {
        _userManager = userManager;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var current = await _userManager.GetUserAsync(User);
        if (current is null) return Challenge();

        var query = _userManager.Users.AsQueryable();
        if (!User.IsInRole(AppRoles.SuperAdmin))
            query = query.Where(x => x.SchoolId == current.SchoolId);

        var users = query.OrderBy(x => x.FullName).ToList();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Roles = string.Join(", ", roles),
                IsActive = user.IsActive
            });
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var current = await _userManager.GetUserAsync(User);
        if (current is null) return Challenge();
        if (!User.IsInRole(AppRoles.SuperAdmin) && !current.SchoolId.HasValue)
        {
            TempData["Info"] = "Configure the School Profile before creating school users.";
            return RedirectToAction("Index", "SchoolSetup");
        }

        LoadRoles();
        return View(new UserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        var current = await _userManager.GetUserAsync(User);
        if (current is null) return Challenge();

        if (!CanAssignRole(model.Role))
            ModelState.AddModelError(nameof(model.Role), "You are not allowed to assign this role.");

        if (!User.IsInRole(AppRoles.SuperAdmin) && !current.SchoolId.HasValue)
            ModelState.AddModelError(string.Empty, "Configure the School Profile before creating school users.");

        if (!ModelState.IsValid)
        {
            LoadRoles(model.Role);
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber?.Trim(),
            IsActive = true,
            ForcePasswordChange = model.ForcePasswordChange,
            SchoolId = current.SchoolId
        };

        var create = await _userManager.CreateAsync(user, model.TemporaryPassword);
        if (!create.Succeeded)
        {
            foreach (var error in create.Errors) ModelState.AddModelError(string.Empty, error.Description);
            LoadRoles(model.Role);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            LoadRoles(model.Role);
            return View(model);
        }

        await _audit.WriteAsync("User.Created", "ApplicationUser", user.Id, $"Role={model.Role}; Active=true; SchoolId={user.SchoolId}");
        TempData["Success"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await CanAccessUserAsync(user)) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? string.Empty;
        if (!CanManageTargetRole(currentRole)) return Forbid();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = currentRole,
            IsActive = user.IsActive
        };

        LoadRoles(model.Role);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null || !await CanAccessUserAsync(user)) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault() ?? string.Empty;
        if (!CanManageTargetRole(currentRole) || !CanAssignRole(model.Role)) return Forbid();

        if (!ModelState.IsValid)
        {
            LoadRoles(model.Role);
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.IsActive = model.IsActive;

        var email = model.Email.Trim();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(user, email);
            if (!emailResult.Succeeded) { AddErrors(emailResult); LoadRoles(model.Role); return View(model); }
            var userNameResult = await _userManager.SetUserNameAsync(user, email);
            if (!userNameResult.Succeeded) { AddErrors(userNameResult); LoadRoles(model.Role); return View(model); }
        }

        var phoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber?.Trim());
        if (!phoneResult.Succeeded) { AddErrors(phoneResult); LoadRoles(model.Role); return View(model); }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) { AddErrors(updateResult); LoadRoles(model.Role); return View(model); }

        if (!currentRoles.Contains(model.Role))
        {
            if (currentRoles.Count > 0) await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        if (!user.IsActive) await _userManager.UpdateSecurityStampAsync(user);
        await _audit.WriteAsync("User.Updated", "ApplicationUser", user.Id, $"Role={model.Role}; Active={model.IsActive}; SchoolId={user.SchoolId}");

        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTemporaryPassword(string id, string temporaryPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await CanAccessUserAsync(user)) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        if (!CanManageTargetRole(roles.FirstOrDefault() ?? string.Empty)) return Forbid();
        if (string.IsNullOrWhiteSpace(temporaryPassword))
        {
            TempData["Error"] = "Temporary password is required.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, temporaryPassword);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Edit), new { id });
        }

        user.ForcePasswordChange = true;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        await _audit.WriteAsync("User.TemporaryPassword.Set", "ApplicationUser", user.Id);

        TempData["Success"] = "Temporary password set. User must change it after login.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<bool> CanAccessUserAsync(ApplicationUser target)
    {
        if (User.IsInRole(AppRoles.SuperAdmin)) return true;
        var current = await _userManager.GetUserAsync(User);
        return current?.SchoolId.HasValue == true && target.SchoolId == current.SchoolId;
    }

    private void LoadRoles(string? selected = null)
    {
        ViewBag.Roles = GetAssignableRoles().Select(r => new SelectListItem(r, r, r == selected)).ToList();
    }

    private IEnumerable<string> GetAssignableRoles()
    {
        if (User.IsInRole(AppRoles.SuperAdmin)) return AppRoles.All;
        if (User.IsInRole(AppRoles.Principal)) return AppRoles.All.Where(r => r != AppRoles.SuperAdmin);
        return AppRoles.All.Where(r => r != AppRoles.SuperAdmin && r != AppRoles.Principal);
    }

    private bool CanAssignRole(string role) => GetAssignableRoles().Contains(role);

    private bool CanManageTargetRole(string role)
    {
        if (User.IsInRole(AppRoles.SuperAdmin)) return true;
        if (role == AppRoles.SuperAdmin) return false;
        if (User.IsInRole(AppRoles.Principal)) return true;
        return role != AppRoles.Principal;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
    }
}
