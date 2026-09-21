using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
public class SchoolSetupController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditService _audit;

    public SchoolSetupController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _environment = environment;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        School? school = null;
        if (user.SchoolId.HasValue)
        {
            school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.SchoolId.Value);
        }
        else if (await _db.Schools.CountAsync() == 1)
        {
            school = await _db.Schools.AsNoTracking().FirstAsync();
        }

        var model = school is null
            ? new SchoolProfileViewModel()
            : new SchoolProfileViewModel
            {
                Id = school.Id,
                Name = school.Name,
                RegistrationNumber = school.RegistrationNumber,
                Address = school.Address,
                Phone = school.Phone,
                Email = school.Email,
                PrincipalName = school.PrincipalName,
                ExistingLogoPath = school.LogoPath,
                ChallanFooterText = school.ChallanFooterText,
                ReceiptFooterText = school.ReceiptFooterText
            };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Index(SchoolProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (model.Logo is not null)
        {
            if (model.Logo.Length > 2 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.Logo), "Logo must be 2 MB or smaller.");

            var extension = Path.GetExtension(model.Logo.FileName).ToLowerInvariant();
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            if (!allowed.Contains(extension))
                ModelState.AddModelError(nameof(model.Logo), "Allowed logo types: PNG, JPG, JPEG, WEBP.");
        }

        if (!ModelState.IsValid)
            return View(model);

        School? school = null;
        if (user.SchoolId.HasValue)
            school = await _db.Schools.FirstOrDefaultAsync(x => x.Id == user.SchoolId.Value);
        else if (model.Id > 0)
            school = await _db.Schools.FirstOrDefaultAsync(x => x.Id == model.Id);
        else if (await _db.Schools.CountAsync() == 1)
            school = await _db.Schools.FirstAsync();

        var isNew = school is null;
        if (school is null)
        {
            school = new School();
            _db.Schools.Add(school);
        }

        school.Name = model.Name.Trim();
        school.RegistrationNumber = model.RegistrationNumber?.Trim();
        school.Address = model.Address?.Trim();
        school.Phone = model.Phone?.Trim();
        school.Email = model.Email?.Trim();
        school.PrincipalName = model.PrincipalName?.Trim();
        school.ChallanFooterText = model.ChallanFooterText?.Trim();
        school.ReceiptFooterText = model.ReceiptFooterText?.Trim();
        school.UpdatedAtUtc = DateTime.UtcNow;

        if (model.Logo is not null)
        {
            var uploads = Path.Combine(_environment.WebRootPath, "uploads", "school");
            Directory.CreateDirectory(uploads);
            var extension = Path.GetExtension(model.Logo.FileName).ToLowerInvariant();
            var safeFileName = $"school-logo-{Guid.NewGuid():N}{extension}";
            var diskPath = Path.Combine(uploads, safeFileName);
            await using var stream = System.IO.File.Create(diskPath);
            await model.Logo.CopyToAsync(stream);
            school.LogoPath = "/uploads/school/" + safeFileName;
        }

        await _db.SaveChangesAsync();

        // Link the initial principal/admin and all pre-M02 unassigned users to the school.
        if (!user.SchoolId.HasValue)
        {
            var unassignedUsers = await _db.Users.Where(x => x.SchoolId == null).ToListAsync();
            foreach (var unassigned in unassignedUsers)
                unassigned.SchoolId = school.Id;

            await _db.SaveChangesAsync();
        }

        await _audit.WriteAsync(isNew ? "School.Created" : "School.Updated", "School", school.Id.ToString(), school.Name);
        TempData["Success"] = isNew ? "School profile created successfully." : "School profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
