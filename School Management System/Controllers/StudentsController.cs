using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Receptionist)]
public class StudentsController : Controller
{
    private const string EditRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Receptionist;

    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IStudentService _studentService;
    private readonly IStudentFileService _fileService;
    private readonly IAuditService _audit;

    public StudentsController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IStudentService studentService,
        IStudentFileService fileService,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _studentService = studentService;
        _fileService = fileService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        StudentStatus? status,
        string? className,
        int? academicSessionId)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var query = _db.Students
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.StudentGuardians)
                .ThenInclude(x => x.Guardian)
            .Include(x => x.Enrollments)
                .ThenInclude(x => x.AcademicSession)
            .Where(x => x.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.FullName.Contains(term) ||
                x.AdmissionNumber.Contains(term) ||
                (x.RollNumber != null && x.RollNumber.Contains(term)) ||
                (x.BFormCnic != null && x.BFormCnic.Contains(term)) ||
                x.StudentGuardians.Any(g => g.Guardian.Phone.Contains(term)));
        }

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(className))
        {
            var cls = className.Trim();
            query = query.Where(x => x.Enrollments.Any(e => e.IsCurrent && e.ClassName == cls));
        }

        if (academicSessionId.HasValue)
            query = query.Where(x => x.Enrollments.Any(e => e.IsCurrent && e.AcademicSessionId == academicSessionId.Value));

        var students = await query
            .OrderBy(x => x.FullName)
            .Take(500)
            .ToListAsync();

        var all = _db.Students.AsNoTracking().Where(x => x.SchoolId == schoolId.Value);
        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId.Value)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
        var classes = await _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId.Value && x.IsCurrent)
            .Select(x => x.ClassName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return View(new StudentIndexViewModel
        {
            Students = students,
            Sessions = sessions,
            Classes = classes,
            Query = q,
            Status = status,
            ClassName = className,
            AcademicSessionId = academicSessionId,
            TotalStudents = await all.CountAsync(),
            ActiveStudents = await all.CountAsync(x => x.Status == StudentStatus.Active),
            InactiveStudents = await all.CountAsync(x => x.Status != StudentStatus.Active)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var student = await _db.Students
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.StudentGuardians)
                .ThenInclude(x => x.Guardian)
            .Include(x => x.Documents)
            .Include(x => x.Enrollments)
                .ThenInclude(x => x.AcademicSession)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value);

        if (student is null) return NotFound();

        var history = student.Enrollments
            .OrderByDescending(x => x.IsCurrent)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToList();

        var model = new StudentDetailsViewModel
        {
            Student = student,
            CurrentEnrollment = history.FirstOrDefault(x => x.IsCurrent),
            EnrollmentHistory = history,
            Guardians = student.StudentGuardians.OrderByDescending(x => x.IsPrimary).ToList(),
            Documents = student.Documents.OrderByDescending(x => x.UploadedAtUtc).ToList()
        };

        return View(model);
    }

    [Authorize(Roles = EditRoles)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await FindOwnedStudentAsync(id, asTracking: false);
        if (student is null) return NotFound();

        return View(new StudentEditViewModel
        {
            Id = student.Id,
            FullName = student.FullName,
            FatherGuardianName = student.FatherGuardianName,
            Gender = student.Gender,
            DateOfBirth = student.DateOfBirth,
            BFormCnic = student.BFormCnic,
            Address = student.Address,
            ContactNumber = student.ContactNumber,
            AdmissionDate = student.AdmissionDate,
            Status = student.Status,
            RowVersion = Convert.ToBase64String(student.RowVersion ?? [])
        });
    }

    [Authorize(Roles = EditRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentEditViewModel model)
    {
        var student = await FindOwnedStudentAsync(model.Id, asTracking: true);
        if (student is null) return NotFound();

        if (model.DateOfBirth.Date >= DateTime.Today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth must be before today.");
        if (model.AdmissionDate.Date < model.DateOfBirth.Date)
            ModelState.AddModelError(nameof(model.AdmissionDate), "Admission date cannot be before date of birth.");

        if (!ModelState.IsValid)
            return View(model);

        student.FullName = model.FullName.Trim();
        student.FatherGuardianName = NullIfBlank(model.FatherGuardianName);
        student.Gender = NullIfBlank(model.Gender);
        student.DateOfBirth = model.DateOfBirth.Date;
        student.BFormCnic = NullIfBlank(model.BFormCnic);
        student.Address = NullIfBlank(model.Address);
        student.ContactNumber = NullIfBlank(model.ContactNumber);
        student.AdmissionDate = model.AdmissionDate.Date;
        student.Status = model.Status;
        student.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(model.RowVersion))
        {
            try
            {
                _db.Entry(student).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(model.RowVersion);
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "The record version is invalid. Reload the page and try again.");
                return View(model);
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Another user changed this student record. Reload the page before saving again.");
            return View(model);
        }

        await _audit.WriteAsync("Student.Updated", "Student", student.Id.ToString(), $"AdmissionNo={student.AdmissionNumber}; Status={student.Status}");
        TempData["Success"] = "Student profile updated.";
        return RedirectToAction(nameof(Details), new { id = student.Id });
    }

    [Authorize(Roles = EditRoles)]
    [HttpGet]
    public async Task<IActionResult> ChangePlacement(int id)
    {
        var student = await FindOwnedStudentAsync(id, asTracking: false);
        if (student is null) return NotFound();

        var current = await _db.StudentEnrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StudentId == id && x.IsCurrent);

        var schoolId = student.SchoolId;
        var preferredSessionId = current?.AcademicSessionId ?? await GetPreferredSessionIdAsync(schoolId);
        await LoadPlacementListsAsync(schoolId, current?.AcademicSessionId, current?.SchoolClassId, current?.SectionId, current?.AcademicGroupId);
        ViewBag.Student = student;

        var classId = current?.SchoolClassId;
        if (!classId.HasValue && current is not null)
        {
            classId = await _db.SchoolClasses.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.Name == current.ClassName)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        return View(new StudentEnrollmentFormViewModel
        {
            StudentId = student.Id,
            AcademicSessionId = preferredSessionId,
            SchoolClassId = classId ?? 0,
            SectionId = current?.SectionId,
            AcademicGroupId = current?.AcademicGroupId,
            RollNumber = current?.RollNumber ?? student.RollNumber,
            EffectiveFrom = current is null ? student.AdmissionDate.Date : DateTime.Today,
            Status = StudentEnrollmentStatus.Active
        });
    }

    [Authorize(Roles = EditRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlacement(StudentEnrollmentFormViewModel model)
    {
        var student = await FindOwnedStudentAsync(model.StudentId, asTracking: false);
        if (student is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadPlacementListsAsync(student.SchoolId, model.AcademicSessionId, model.SchoolClassId, model.SectionId, model.AcademicGroupId);
            ViewBag.Student = student;
            return View(model);
        }

        var result = await _studentService.ChangePlacementAsync(
            student.Id,
            student.SchoolId,
            model.AcademicSessionId,
            model.SchoolClassId,
            model.SectionId,
            model.AcademicGroupId,
            model.RollNumber,
            model.EffectiveFrom,
            model.Status,
            model.Notes);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await LoadPlacementListsAsync(student.SchoolId, model.AcademicSessionId, model.SchoolClassId, model.SectionId, model.AcademicGroupId);
            ViewBag.Student = student;
            return View(model);
        }

        await _audit.WriteAsync("Student.PlacementChanged", "StudentEnrollment", result.Enrollment!.Id.ToString(),
            $"StudentId={student.Id}; SessionId={model.AcademicSessionId}; ClassId={model.SchoolClassId}; SectionId={model.SectionId}; Roll={model.RollNumber}");
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = student.Id });
    }

    [Authorize(Roles = EditRoles)]
    [HttpGet]
    public async Task<IActionResult> AddGuardian(int id)
    {
        var student = await FindOwnedStudentAsync(id, asTracking: false);
        if (student is null) return NotFound();
        ViewBag.Student = student;
        return View("GuardianForm", new StudentGuardianFormViewModel { StudentId = id });
    }

    [Authorize(Roles = EditRoles)]
    [HttpGet]
    public async Task<IActionResult> EditGuardian(int studentId, int guardianId)
    {
        var link = await FindOwnedGuardianLinkAsync(studentId, guardianId);
        if (link is null) return NotFound();

        ViewBag.Student = link.Student;
        return View("GuardianForm", new StudentGuardianFormViewModel
        {
            StudentId = studentId,
            GuardianId = guardianId,
            FullName = link.Guardian.FullName,
            Relationship = link.Guardian.Relationship,
            Phone = link.Guardian.Phone,
            Occupation = link.Guardian.Occupation,
            Cnic = link.Guardian.Cnic,
            Address = link.Guardian.Address,
            IsPrimary = link.IsPrimary
        });
    }

    [Authorize(Roles = EditRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGuardian(StudentGuardianFormViewModel model)
    {
        var student = await FindOwnedStudentAsync(model.StudentId, asTracking: true);
        if (student is null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Student = student;
            return View("GuardianForm", model);
        }

        StudentGuardian link;
        if (model.GuardianId == 0)
        {
            var guardian = new Guardian
            {
                SchoolId = student.SchoolId,
                FullName = model.FullName.Trim(),
                Relationship = model.Relationship.Trim(),
                Phone = model.Phone.Trim(),
                Occupation = NullIfBlank(model.Occupation),
                Cnic = NullIfBlank(model.Cnic),
                Address = NullIfBlank(model.Address)
            };
            _db.Guardians.Add(guardian);
            await _db.SaveChangesAsync();

            link = new StudentGuardian
            {
                StudentId = student.Id,
                GuardianId = guardian.Id,
                IsPrimary = model.IsPrimary
            };
            _db.StudentGuardians.Add(link);
        }
        else
        {
            var existing = await _db.StudentGuardians
                .Include(x => x.Guardian)
                .FirstOrDefaultAsync(x => x.StudentId == student.Id && x.GuardianId == model.GuardianId && x.Guardian.SchoolId == student.SchoolId);
            if (existing is null) return NotFound();

            existing.Guardian.FullName = model.FullName.Trim();
            existing.Guardian.Relationship = model.Relationship.Trim();
            existing.Guardian.Phone = model.Phone.Trim();
            existing.Guardian.Occupation = NullIfBlank(model.Occupation);
            existing.Guardian.Cnic = NullIfBlank(model.Cnic);
            existing.Guardian.Address = NullIfBlank(model.Address);
            existing.IsPrimary = model.IsPrimary;
            link = existing;
        }

        if (model.IsPrimary)
        {
            var otherLinks = await _db.StudentGuardians
                .Where(x => x.StudentId == student.Id && x.GuardianId != link.GuardianId)
                .ToListAsync();
            foreach (var other in otherLinks)
                other.IsPrimary = false;

            student.FatherGuardianName = model.FullName.Trim();
            student.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.WriteAsync(model.GuardianId == 0 ? "Student.GuardianAdded" : "Student.GuardianUpdated",
            "Guardian", link.GuardianId.ToString(), $"StudentId={student.Id}; Primary={link.IsPrimary}");

        TempData["Success"] = "Guardian details saved.";
        return RedirectToAction(nameof(Details), new { id = student.Id });
    }

    [Authorize(Roles = EditRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(StudentDocumentUploadViewModel model)
    {
        var student = await FindOwnedStudentAsync(model.StudentId, asTracking: false);
        if (student is null) return NotFound();

        if (model.File is null)
        {
            TempData["Error"] = "Choose a document to upload.";
            return RedirectToAction(nameof(Details), new { id = model.StudentId, tab = "documents" });
        }

        var saved = await _fileService.SaveDocumentAsync(model.File, student.SchoolId, student.Id);
        if (!saved.Success)
        {
            TempData["Error"] = saved.Message;
            return RedirectToAction(nameof(Details), new { id = student.Id, tab = "documents" });
        }

        var document = new StudentDocument
        {
            StudentId = student.Id,
            DocumentType = model.DocumentType,
            OriginalFileName = Path.GetFileName(model.File.FileName),
            StorageKey = saved.StorageKey!,
            ContentType = saved.ContentType!,
            SizeBytes = saved.SizeBytes,
            UploadedAtUtc = DateTime.UtcNow
        };
        _db.StudentDocuments.Add(document);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("Student.DocumentUploaded", "StudentDocument", document.Id.ToString(), $"StudentId={student.Id}; Type={document.DocumentType}");

        TempData["Success"] = "Student document uploaded.";
        return RedirectToAction(nameof(Details), new { id = student.Id, tab = "documents" });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var document = await _db.StudentDocuments.AsNoTracking()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id && x.Student.SchoolId == schoolId.Value);
        if (document is null) return NotFound();

        var opened = await _fileService.OpenReadAsync(document.StorageKey);
        if (opened.Stream is null) return NotFound();
        return File(opened.Stream, opened.ContentType, document.OriginalFileName);
    }

    [Authorize(Roles = EditRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var document = await _db.StudentDocuments
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id && x.Student.SchoolId == schoolId.Value);
        if (document is null) return NotFound();

        var studentId = document.StudentId;
        var storageKey = document.StorageKey;
        _db.StudentDocuments.Remove(document);
        await _db.SaveChangesAsync();
        await _fileService.DeleteAsync(storageKey);
        await _audit.WriteAsync("Student.DocumentDeleted", "StudentDocument", id.ToString(), $"StudentId={studentId}");

        TempData["Success"] = "Document removed from the student profile.";
        return RedirectToAction(nameof(Details), new { id = studentId, tab = "documents" });
    }

    [Authorize(Roles = EditRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(int studentId, IFormFile? photo)
    {
        var student = await FindOwnedStudentAsync(studentId, asTracking: true);
        if (student is null) return NotFound();
        if (photo is null)
        {
            TempData["Error"] = "Choose a photo first.";
            return RedirectToAction(nameof(Details), new { id = studentId });
        }

        var saved = await _fileService.SavePhotoAsync(photo, student.SchoolId, student.Id);
        if (!saved.Success)
        {
            TempData["Error"] = saved.Message;
            return RedirectToAction(nameof(Details), new { id = studentId });
        }

        var old = student.PhotoStorageKey;
        student.PhotoStorageKey = saved.StorageKey;
        student.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        if (!string.IsNullOrWhiteSpace(old))
            await _fileService.DeleteAsync(old);
        await _audit.WriteAsync("Student.PhotoUpdated", "Student", student.Id.ToString());

        TempData["Success"] = "Student photo updated.";
        return RedirectToAction(nameof(Details), new { id = studentId });
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int id)
    {
        var student = await FindOwnedStudentAsync(id, asTracking: false);
        if (student is null || string.IsNullOrWhiteSpace(student.PhotoStorageKey)) return NotFound();

        var opened = await _fileService.OpenReadAsync(student.PhotoStorageKey);
        return opened.Stream is null ? NotFound() : File(opened.Stream, opened.ContentType);
    }

    private async Task<Student?> FindOwnedStudentAsync(int id, bool asTracking)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return null;

        var query = _db.Students.Where(x => x.Id == id && x.SchoolId == schoolId.Value);
        if (!asTracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync();
    }

    private async Task<StudentGuardian?> FindOwnedGuardianLinkAsync(int studentId, int guardianId)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return null;

        return await _db.StudentGuardians
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Guardian)
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.GuardianId == guardianId && x.Student.SchoolId == schoolId.Value);
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
        TempData["Info"] = "Configure the School Profile before using Student Management.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private async Task LoadSessionsAsync(int schoolId, int? selected)
    {
        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status != AcademicSessionStatus.Archived)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
        ViewBag.AcademicSessions = new SelectList(sessions, "Id", "Name", selected);
    }

    private async Task LoadPlacementListsAsync(int schoolId, int? selectedSessionId, int? selectedClassId, int? selectedSectionId, int? selectedGroupId)
    {
        await LoadSessionsAsync(schoolId, selectedSessionId);
        ViewBag.Classes = await _db.SchoolClasses.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        ViewBag.Sections = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SchoolClassId).ThenBy(x => x.Name).ToListAsync();
        ViewBag.Groups = await _db.AcademicGroups.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SchoolClassId).ThenBy(x => x.Name).ToListAsync();
        ViewBag.SelectedClassId = selectedClassId;
        ViewBag.SelectedSectionId = selectedSectionId;
        ViewBag.SelectedGroupId = selectedGroupId;
    }

    private async Task<int> GetPreferredSessionIdAsync(int schoolId)
    {
        return await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status != AcademicSessionStatus.Archived)
            .OrderByDescending(x => x.Status == AcademicSessionStatus.Active)
            .ThenByDescending(x => x.StartDate)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
