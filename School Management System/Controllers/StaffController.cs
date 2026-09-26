using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.HR)]
public class StaffController : Controller
{
    private const string ManageRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.HR;
    private const string AccountLinkRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin;

    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IStaffService _staffService;
    private readonly IStaffFileService _fileService;
    private readonly IAuditService _audit;
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IStaffService staffService,
        IStaffFileService fileService,
        IAuditService audit,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _schoolContext = schoolContext;
        _staffService = staffService;
        _fileService = fileService;
        _audit = audit;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        StaffStatus? status,
        StaffEmploymentType? employmentType,
        string? department)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var query = _db.Staff.AsNoTracking().Where(x => x.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.FullName.Contains(term) ||
                x.EmployeeId.Contains(term) ||
                (x.Cnic != null && x.Cnic.Contains(term)) ||
                (x.Phone != null && x.Phone.Contains(term)) ||
                x.Designation.Contains(term));
        }

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (employmentType.HasValue)
            query = query.Where(x => x.EmploymentType == employmentType.Value);

        if (!string.IsNullOrWhiteSpace(department))
        {
            var value = department.Trim();
            query = query.Where(x => x.Department == value);
        }

        var staffMembers = await query
            .OrderBy(x => x.FullName)
            .Take(500)
            .ToListAsync();

        var all = _db.Staff.AsNoTracking().Where(x => x.SchoolId == schoolId.Value);
        var departments = await all
            .Where(x => x.Department != null && x.Department != "")
            .Select(x => x.Department!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var linkedTeacherStaffIds = await _db.Users.AsNoTracking()
            .Where(x => x.SchoolId == schoolId.Value && x.StaffId.HasValue)
            .Join(_db.UserRoles,
                user => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user.StaffId, userRole.RoleId })
            .Join(_db.Roles.Where(x => x.Name == AppRoles.Teacher),
                joined => joined.RoleId,
                role => role.Id,
                (joined, role) => joined.StaffId!.Value)
            .Distinct()
            .ToListAsync();

        return View(new StaffIndexViewModel
        {
            StaffMembers = staffMembers,
            Query = q,
            Status = status,
            EmploymentType = employmentType,
            Department = department,
            Departments = departments,
            TotalStaff = await all.CountAsync(),
            ActiveStaff = await all.CountAsync(x => x.Status == StaffStatus.Active || x.Status == StaffStatus.OnLeave),
            TeachingStaff = linkedTeacherStaffIds.Count
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var staff = await _db.Staff
            .AsNoTracking()
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value);
        if (staff is null) return NotFound();

        var linkedUser = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId.Value && x.StaffId == staff.Id);

        IReadOnlyList<string> roles = [];
        IReadOnlyList<TeacherAssignment> teacherAssignments = [];
        IReadOnlyList<Section> classTeacherSections = [];

        if (linkedUser is not null)
        {
            roles = (await _userManager.GetRolesAsync(linkedUser)).ToList();

            teacherAssignments = await _db.TeacherAssignments
                .AsNoTracking()
                .Include(x => x.AcademicSession)
                .Include(x => x.SchoolClass)
                .Include(x => x.Section)
                .Include(x => x.Subject)
                .Where(x => x.SchoolId == schoolId.Value && x.TeacherUserId == linkedUser.Id && x.IsActive)
                .OrderByDescending(x => x.AcademicSession.StartDate)
                .ThenBy(x => x.SchoolClass.SortOrder)
                .ThenBy(x => x.Subject.Title)
                .ToListAsync();

            classTeacherSections = await _db.Sections
                .AsNoTracking()
                .Include(x => x.SchoolClass)
                .Where(x => x.SchoolId == schoolId.Value && x.IsActive && x.ClassTeacherUserId == linkedUser.Id)
                .OrderBy(x => x.SchoolClass.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        var availableAccounts = new List<ApplicationUser>();
        if (User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin))
        {
            availableAccounts = await _db.Users.AsNoTracking()
                .Where(x => x.SchoolId == schoolId.Value && x.IsActive && (!x.StaffId.HasValue || x.StaffId == staff.Id))
                .OrderBy(x => x.FullName)
                .ToListAsync();
        }

        return View(new StaffDetailsViewModel
        {
            Staff = staff,
            LinkedUser = linkedUser,
            LinkedUserRoles = roles,
            AvailableUserAccounts = availableAccounts,
            TeacherAssignments = teacherAssignments,
            ClassTeacherSections = classTeacherSections,
            Documents = staff.Documents.OrderByDescending(x => x.UploadedAtUtc).ToList()
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        return View(new StaffFormViewModel { JoiningDate = DateTime.Today, Status = StaffStatus.Active });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        ValidateStaffForm(model, isCreate: true);
        if (!ModelState.IsValid)
            return View(model);

        var staff = new Staff
        {
            SchoolId = schoolId.Value,
            FullName = model.FullName.Trim(),
            Cnic = NullIfBlank(model.Cnic),
            Phone = NullIfBlank(model.Phone),
            Email = NullIfBlank(model.Email),
            Address = NullIfBlank(model.Address),
            Designation = model.Designation.Trim(),
            Department = NullIfBlank(model.Department),
            Qualification = NullIfBlank(model.Qualification),
            JoiningDate = model.JoiningDate.Date,
            EmploymentType = model.EmploymentType,
            Status = StaffStatus.Active,
            Notes = NullIfBlank(model.Notes)
        };

        var created = await _staffService.CreateAsync(staff);
        if (!created.Success || !created.StaffId.HasValue)
        {
            ModelState.AddModelError(string.Empty, created.Message);
            return View(model);
        }

        await _audit.WriteAsync("Staff.Created", "Staff", created.StaffId.Value.ToString(), $"EmployeeId={created.EmployeeId}; Designation={staff.Designation}");
        TempData["Success"] = created.Message;
        return RedirectToAction(nameof(Details), new { id = created.StaffId.Value });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var staff = await FindOwnedStaffAsync(id, asTracking: false);
        if (staff is null) return NotFound();

        return View(new StaffFormViewModel
        {
            Id = staff.Id,
            EmployeeId = staff.EmployeeId,
            FullName = staff.FullName,
            Cnic = staff.Cnic,
            Phone = staff.Phone,
            Email = staff.Email,
            Address = staff.Address,
            Designation = staff.Designation,
            Department = staff.Department,
            Qualification = staff.Qualification,
            JoiningDate = staff.JoiningDate,
            EmploymentType = staff.EmploymentType,
            Status = staff.Status,
            Notes = staff.Notes,
            RowVersion = Convert.ToBase64String(staff.RowVersion ?? [])
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StaffFormViewModel model)
    {
        var staff = await FindOwnedStaffAsync(model.Id, asTracking: true);
        if (staff is null) return NotFound();

        ValidateStaffForm(model, isCreate: false);
        if (!ModelState.IsValid)
        {
            model.EmployeeId = staff.EmployeeId;
            return View(model);
        }

        staff.FullName = model.FullName.Trim();
        staff.Cnic = NullIfBlank(model.Cnic);
        staff.Phone = NullIfBlank(model.Phone);
        staff.Email = NullIfBlank(model.Email);
        staff.Address = NullIfBlank(model.Address);
        staff.Designation = model.Designation.Trim();
        staff.Department = NullIfBlank(model.Department);
        staff.Qualification = NullIfBlank(model.Qualification);
        staff.JoiningDate = model.JoiningDate.Date;
        staff.EmploymentType = model.EmploymentType;
        staff.Notes = NullIfBlank(model.Notes);
        staff.UpdatedAtUtc = DateTime.UtcNow;

        // Exit statuses are changed through Mark Exit so a date/reason cannot be accidentally omitted.
        if (staff.ExitDate is null && model.Status is StaffStatus.Active or StaffStatus.OnLeave or StaffStatus.Inactive)
            staff.Status = model.Status;

        if (!string.IsNullOrWhiteSpace(model.RowVersion))
        {
            try
            {
                _db.Entry(staff).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(model.RowVersion);
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "The record version is invalid. Reload the page and try again.");
                model.EmployeeId = staff.EmployeeId;
                return View(model);
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Another user changed this staff record. Reload the page before saving again.");
            model.EmployeeId = staff.EmployeeId;
            return View(model);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "CNIC or another unique staff value may already exist. Check the record and try again.");
            model.EmployeeId = staff.EmployeeId;
            return View(model);
        }

        await _audit.WriteAsync("Staff.Updated", "Staff", staff.Id.ToString(), $"EmployeeId={staff.EmployeeId}; Status={staff.Status}");
        TempData["Success"] = "Staff profile updated.";
        return RedirectToAction(nameof(Details), new { id = staff.Id });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> MarkExit(int id)
    {
        var staff = await FindOwnedStaffAsync(id, asTracking: false);
        if (staff is null) return NotFound();

        return View(new StaffExitViewModel
        {
            StaffId = staff.Id,
            EmployeeId = staff.EmployeeId,
            FullName = staff.FullName,
            ExitDate = staff.ExitDate ?? DateTime.Today,
            Status = staff.Status is StaffStatus.Resigned or StaffStatus.Terminated or StaffStatus.Retired
                ? staff.Status
                : StaffStatus.Resigned,
            ExitReason = staff.ExitReason ?? string.Empty
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkExit(StaffExitViewModel model)
    {
        var staff = await FindOwnedStaffAsync(model.StaffId, asTracking: true);
        if (staff is null) return NotFound();

        if (model.Status is not (StaffStatus.Resigned or StaffStatus.Terminated or StaffStatus.Retired))
            ModelState.AddModelError(nameof(model.Status), "Choose Resigned, Terminated or Retired as the exit status.");

        if (model.ExitDate.Date < staff.JoiningDate.Date)
            ModelState.AddModelError(nameof(model.ExitDate), "Exit date cannot be before the joining date.");

        if (!ModelState.IsValid)
        {
            model.EmployeeId = staff.EmployeeId;
            model.FullName = staff.FullName;
            return View(model);
        }

        staff.Status = model.Status;
        staff.ExitDate = model.ExitDate.Date;
        staff.ExitReason = model.ExitReason.Trim();
        staff.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("Staff.ExitRecorded", "Staff", staff.Id.ToString(), $"EmployeeId={staff.EmployeeId}; Status={staff.Status}; ExitDate={staff.ExitDate:yyyy-MM-dd}");
        TempData["Success"] = "Staff exit recorded. The historical profile has been preserved.";
        return RedirectToAction(nameof(Details), new { id = staff.Id });
    }

    [Authorize(Roles = AccountLinkRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkUser(int staffId, string? userId)
    {
        var staff = await FindOwnedStaffAsync(staffId, asTracking: false);
        if (staff is null) return NotFound();
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "Select a user account to link.";
            return RedirectToAction(nameof(Details), new { id = staffId });
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.SchoolId == staff.SchoolId && x.IsActive);
        if (user is null) return NotFound();

        if (user.StaffId.HasValue && user.StaffId.Value != staffId)
        {
            TempData["Error"] = "That user account is already linked to another staff member.";
            return RedirectToAction(nameof(Details), new { id = staffId });
        }

        var previous = await _db.Users.FirstOrDefaultAsync(x => x.SchoolId == staff.SchoolId && x.StaffId == staffId && x.Id != user.Id);
        if (previous is not null)
            previous.StaffId = null;

        user.StaffId = staffId;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("Staff.UserLinked", "Staff", staffId.ToString(), $"User={user.Email}");

        TempData["Success"] = "User account linked to the staff profile.";
        return RedirectToAction(nameof(Details), new { id = staffId });
    }

    [Authorize(Roles = AccountLinkRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkUser(int staffId)
    {
        var staff = await FindOwnedStaffAsync(staffId, asTracking: false);
        if (staff is null) return NotFound();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.SchoolId == staff.SchoolId && x.StaffId == staffId);
        if (user is not null)
        {
            user.StaffId = null;
            await _db.SaveChangesAsync();
            await _audit.WriteAsync("Staff.UserUnlinked", "Staff", staffId.ToString(), $"User={user.Email}");
        }

        TempData["Success"] = "User account link removed. The staff profile was not deleted.";
        return RedirectToAction(nameof(Details), new { id = staffId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(int staffId, IFormFile? photo)
    {
        var staff = await FindOwnedStaffAsync(staffId, asTracking: true);
        if (staff is null) return NotFound();
        if (photo is null)
        {
            TempData["Error"] = "Choose a photo first.";
            return RedirectToAction(nameof(Details), new { id = staffId });
        }

        var saved = await _fileService.SavePhotoAsync(photo, staff.SchoolId, staff.Id);
        if (!saved.Success)
        {
            TempData["Error"] = saved.Message;
            return RedirectToAction(nameof(Details), new { id = staffId });
        }

        var old = staff.PhotoStorageKey;
        staff.PhotoStorageKey = saved.StorageKey;
        staff.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(old))
            await _fileService.DeleteAsync(old);

        await _audit.WriteAsync("Staff.PhotoUpdated", "Staff", staff.Id.ToString());
        TempData["Success"] = "Staff photo updated.";
        return RedirectToAction(nameof(Details), new { id = staffId });
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int id)
    {
        var staff = await FindOwnedStaffAsync(id, asTracking: false);
        if (staff is null || string.IsNullOrWhiteSpace(staff.PhotoStorageKey)) return NotFound();

        var opened = await _fileService.OpenReadAsync(staff.PhotoStorageKey);
        return opened.Stream is null ? NotFound() : File(opened.Stream, opened.ContentType);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(StaffDocumentUploadViewModel model)
    {
        var staff = await FindOwnedStaffAsync(model.StaffId, asTracking: false);
        if (staff is null) return NotFound();

        if (model.File is null)
        {
            TempData["Error"] = "Choose a document first.";
            return RedirectToAction(nameof(Details), new { id = staff.Id, tab = "documents" });
        }

        var saved = await _fileService.SaveDocumentAsync(model.File, staff.SchoolId, staff.Id);
        if (!saved.Success)
        {
            TempData["Error"] = saved.Message;
            return RedirectToAction(nameof(Details), new { id = staff.Id, tab = "documents" });
        }

        var document = new StaffDocument
        {
            StaffId = staff.Id,
            DocumentType = model.DocumentType,
            OriginalFileName = Path.GetFileName(model.File.FileName),
            StorageKey = saved.StorageKey!,
            ContentType = saved.ContentType!,
            SizeBytes = saved.SizeBytes,
            UploadedAtUtc = DateTime.UtcNow
        };
        _db.StaffDocuments.Add(document);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("Staff.DocumentUploaded", "StaffDocument", document.Id.ToString(), $"StaffId={staff.Id}; Type={document.DocumentType}");

        TempData["Success"] = "Staff document uploaded.";
        return RedirectToAction(nameof(Details), new { id = staff.Id, tab = "documents" });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var document = await _db.StaffDocuments.AsNoTracking()
            .Include(x => x.Staff)
            .FirstOrDefaultAsync(x => x.Id == id && x.Staff.SchoolId == schoolId.Value);
        if (document is null) return NotFound();

        var opened = await _fileService.OpenReadAsync(document.StorageKey);
        if (opened.Stream is null) return NotFound();
        return File(opened.Stream, opened.ContentType, document.OriginalFileName);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var document = await _db.StaffDocuments
            .Include(x => x.Staff)
            .FirstOrDefaultAsync(x => x.Id == id && x.Staff.SchoolId == schoolId.Value);
        if (document is null) return NotFound();

        var staffId = document.StaffId;
        var storageKey = document.StorageKey;
        _db.StaffDocuments.Remove(document);
        await _db.SaveChangesAsync();
        await _fileService.DeleteAsync(storageKey);
        await _audit.WriteAsync("Staff.DocumentDeleted", "StaffDocument", id.ToString(), $"StaffId={staffId}");

        TempData["Success"] = "Document removed from the staff profile.";
        return RedirectToAction(nameof(Details), new { id = staffId, tab = "documents" });
    }

    private async Task<Staff?> FindOwnedStaffAsync(int id, bool asTracking)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return null;

        var query = _db.Staff.Where(x => x.Id == id && x.SchoolId == schoolId.Value);
        if (!asTracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync();
    }

    private async Task<int?> RequireSchoolIdAsync()
    {
        var id = await _schoolContext.GetCurrentSchoolIdAsync();
        if (id.HasValue) return id;
        var school = await _schoolContext.GetCurrentSchoolAsync();
        return school?.Id;
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Info"] = "Configure the School Profile before using Staff / HR.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private void ValidateStaffForm(StaffFormViewModel model, bool isCreate)
    {
        if (model.JoiningDate.Date > DateTime.Today.AddDays(365))
            ModelState.AddModelError(nameof(model.JoiningDate), "Joining date is too far in the future.");

        if (isCreate && model.Status != StaffStatus.Active)
            model.Status = StaffStatus.Active;
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
