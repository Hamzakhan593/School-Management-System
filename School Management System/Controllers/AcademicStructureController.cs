using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Teacher)]
public class AcademicStructureController : Controller
{
    private const string ManageRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin;

    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IAuditService _audit;
    private readonly UserManager<ApplicationUser> _userManager;

    public AcademicStructureController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IAuditService audit,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _schoolContext = schoolContext;
        _audit = audit;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? sessionId)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId.Value && x.Status != AcademicSessionStatus.Archived)
            .OrderByDescending(x => x.Status == AcademicSessionStatus.Active)
            .ThenByDescending(x => x.StartDate)
            .ToListAsync();

        var selectedSessionId = sessionId ?? sessions.FirstOrDefault()?.Id;

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Include(x => x.Sections)
                .ThenInclude(x => x.ClassTeacherUser)
            .Include(x => x.Groups)
            .Where(x => x.SchoolId == schoolId.Value)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var subjects = await _db.Subjects.AsNoTracking()
            .Where(x => x.SchoolId == schoolId.Value)
            .OrderBy(x => x.Title)
            .ToListAsync();

        var classSubjects = selectedSessionId.HasValue
            ? await _db.ClassSubjects.AsNoTracking()
                .Include(x => x.SchoolClass)
                .Include(x => x.Subject)
                .Where(x => x.SchoolId == schoolId.Value && x.AcademicSessionId == selectedSessionId.Value)
                .OrderBy(x => x.SchoolClass.SortOrder)
                .ThenBy(x => x.Subject.Title)
                .ToListAsync()
            : new List<ClassSubject>();

        var teacherAssignments = selectedSessionId.HasValue
            ? await _db.TeacherAssignments.AsNoTracking()
                .Include(x => x.SchoolClass)
                .Include(x => x.Section)
                .Include(x => x.Subject)
                .Include(x => x.TeacherUser)
                .Where(x => x.SchoolId == schoolId.Value && x.AcademicSessionId == selectedSessionId.Value && x.IsActive)
                .OrderBy(x => x.SchoolClass.SortOrder)
                .ThenBy(x => x.Subject.Title)
                .ToListAsync()
            : new List<TeacherAssignment>();

        var teachers = await _userManager.GetUsersInRoleAsync(AppRoles.Teacher);
        var schoolTeachers = teachers.Where(x => x.SchoolId == schoolId.Value && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToList();

        var isManager = User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin);
        if (!isManager)
        {
            var teacherId = _userManager.GetUserId(User);
            teacherAssignments = teacherAssignments.Where(x => x.TeacherUserId == teacherId).ToList();
            var assignedIds = teacherAssignments.Select(x => x.SchoolClassId).ToHashSet();
            classes = classes.Where(x => assignedIds.Contains(x.Id) || x.Sections.Any(s => s.ClassTeacherUserId == teacherId)).ToList();
            foreach (var schoolClass in classes)
            {
                var classWide = teacherAssignments.Any(x => x.SchoolClassId == schoolClass.Id && x.SectionId == null);
                if (!classWide) schoolClass.Sections = schoolClass.Sections.Where(s => s.ClassTeacherUserId == teacherId || teacherAssignments.Any(a => a.SectionId == s.Id)).ToList();
            }
            var visibleIds = classes.Select(x => x.Id).ToHashSet();
            classSubjects = classSubjects.Where(x => visibleIds.Contains(x.SchoolClassId)).ToList();
            var subjectIds = classSubjects.Select(x => x.SubjectId).ToHashSet();
            subjects = subjects.Where(x => subjectIds.Contains(x.Id)).ToList();
            schoolTeachers = schoolTeachers.Where(x => x.Id == teacherId).ToList();
        }

        return View(new AcademicStructureIndexViewModel
        {
            SelectedSessionId = selectedSessionId,
            Sessions = sessions,
            Classes = classes,
            Subjects = subjects,
            ClassSubjects = classSubjects,
            TeacherAssignments = teacherAssignments,
            Teachers = schoolTeachers,
            LegacyPlacementsUnmapped = await _db.StudentEnrollments.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId.Value && x.SchoolClassId == null)
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClass(SchoolClassFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        if (await _db.SchoolClasses.AnyAsync(x => x.SchoolId == schoolId.Value && x.Name == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "A class with this name already exists.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Index));
        }

        var entity = new SchoolClass
        {
            SchoolId = schoolId.Value,
            Name = model.Name.Trim(),
            Code = NullIfBlank(model.Code),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive
        };
        _db.SchoolClasses.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.ClassCreated", "SchoolClass", entity.Id.ToString(), entity.Name);
        TempData["Success"] = "Class created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> EditClass(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.SchoolClasses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        return View(new SchoolClassFormViewModel { Id = item.Id, Name = item.Name, Code = item.Code, SortOrder = item.SortOrder, IsActive = item.IsActive });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClass(SchoolClassFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.SchoolClasses.FirstOrDefaultAsync(x => x.Id == model.Id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        if (await _db.SchoolClasses.AnyAsync(x => x.SchoolId == schoolId && x.Id != model.Id && x.Name == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "A class with this name already exists.");
        if (!ModelState.IsValid) return View(model);

        item.Name = model.Name.Trim();
        item.Code = NullIfBlank(model.Code);
        item.SortOrder = model.SortOrder;
        item.IsActive = model.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.ClassUpdated", "SchoolClass", item.Id.ToString(), item.Name);
        TempData["Success"] = "Class updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSection(SectionFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        if (!await _db.SchoolClasses.AnyAsync(x => x.Id == model.SchoolClassId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.SchoolClassId), "Select a valid class.");
        if (await _db.Sections.AnyAsync(x => x.SchoolClassId == model.SchoolClassId && x.Name == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "This section already exists for the selected class.");
        if (!string.IsNullOrWhiteSpace(model.ClassTeacherUserId) && !await IsSchoolTeacherAsync(model.ClassTeacherUserId, schoolId.Value))
            ModelState.AddModelError(nameof(model.ClassTeacherUserId), "Select a valid teacher from this school.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Index));
        }

        var entity = new Section
        {
            SchoolId = schoolId.Value,
            SchoolClassId = model.SchoolClassId,
            Name = model.Name.Trim(),
            Capacity = model.Capacity,
            Classroom = NullIfBlank(model.Classroom),
            ClassTeacherUserId = NullIfBlank(model.ClassTeacherUserId),
            IsActive = model.IsActive
        };
        _db.Sections.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.SectionCreated", "Section", entity.Id.ToString(), entity.Name);
        TempData["Success"] = "Section created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> EditSection(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.Sections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        await LoadEditListsAsync(schoolId!.Value, item.SchoolClassId, item.ClassTeacherUserId);
        return View(new SectionFormViewModel
        {
            Id = item.Id, SchoolClassId = item.SchoolClassId, Name = item.Name, Capacity = item.Capacity,
            Classroom = item.Classroom, ClassTeacherUserId = item.ClassTeacherUserId, IsActive = item.IsActive
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSection(SectionFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.Sections.FirstOrDefaultAsync(x => x.Id == model.Id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        if (!await _db.SchoolClasses.AnyAsync(x => x.Id == model.SchoolClassId && x.SchoolId == schoolId))
            ModelState.AddModelError(nameof(model.SchoolClassId), "Select a valid class.");
        if (await _db.Sections.AnyAsync(x => x.Id != model.Id && x.SchoolClassId == model.SchoolClassId && x.Name == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "This section already exists for the selected class.");
        if (!string.IsNullOrWhiteSpace(model.ClassTeacherUserId) && !await IsSchoolTeacherAsync(model.ClassTeacherUserId, schoolId!.Value))
            ModelState.AddModelError(nameof(model.ClassTeacherUserId), "Select a valid teacher from this school.");
        if (!ModelState.IsValid)
        {
            await LoadEditListsAsync(schoolId!.Value, model.SchoolClassId, model.ClassTeacherUserId);
            return View(model);
        }

        item.SchoolClassId = model.SchoolClassId;
        item.Name = model.Name.Trim();
        item.Capacity = model.Capacity;
        item.Classroom = NullIfBlank(model.Classroom);
        item.ClassTeacherUserId = NullIfBlank(model.ClassTeacherUserId);
        item.IsActive = model.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.SectionUpdated", "Section", item.Id.ToString(), item.Name);
        TempData["Success"] = "Section updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(AcademicGroupFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        if (!await _db.SchoolClasses.AnyAsync(x => x.Id == model.SchoolClassId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.SchoolClassId), "Select a valid class.");
        if (await _db.AcademicGroups.AnyAsync(x => x.SchoolClassId == model.SchoolClassId && x.Name == model.Name.Trim()))
            ModelState.AddModelError(nameof(model.Name), "This group/stream already exists for the selected class.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Index));
        }
        var entity = new AcademicGroup { SchoolId = schoolId.Value, SchoolClassId = model.SchoolClassId, Name = model.Name.Trim(), Description = NullIfBlank(model.Description), IsActive = model.IsActive };
        _db.AcademicGroups.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.GroupCreated", "AcademicGroup", entity.Id.ToString(), entity.Name);
        TempData["Success"] = "Group/stream created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(SubjectFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        if (model.DefaultPassMarks.HasValue && model.DefaultMaxMarks.HasValue && model.DefaultPassMarks > model.DefaultMaxMarks)
            ModelState.AddModelError(nameof(model.DefaultPassMarks), "Pass marks cannot exceed max marks.");
        if (await _db.Subjects.AnyAsync(x => x.SchoolId == schoolId.Value && (x.Code == model.Code.Trim() || x.Title == model.Title.Trim())))
            ModelState.AddModelError(nameof(model.Code), "Subject code or title already exists.");
        if (!model.HasTheory && !model.HasPractical)
            ModelState.AddModelError(nameof(model.HasTheory), "A subject must have theory, practical, or both.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Index));
        }
        var entity = new Subject
        {
            SchoolId = schoolId.Value, Code = model.Code.Trim(), Title = model.Title.Trim(), HasTheory = model.HasTheory,
            HasPractical = model.HasPractical, DefaultMaxMarks = model.DefaultMaxMarks, DefaultPassMarks = model.DefaultPassMarks, IsActive = model.IsActive
        };
        _db.Subjects.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.SubjectCreated", "Subject", entity.Id.ToString(), entity.Code);
        TempData["Success"] = "Subject created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> EditSubject(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.Subjects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        return View(new SubjectFormViewModel
        {
            Id = item.Id, Code = item.Code, Title = item.Title, HasTheory = item.HasTheory, HasPractical = item.HasPractical,
            DefaultMaxMarks = item.DefaultMaxMarks, DefaultPassMarks = item.DefaultPassMarks, IsActive = item.IsActive
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSubject(SubjectFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.Subjects.FirstOrDefaultAsync(x => x.Id == model.Id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        if (model.DefaultPassMarks.HasValue && model.DefaultMaxMarks.HasValue && model.DefaultPassMarks > model.DefaultMaxMarks)
            ModelState.AddModelError(nameof(model.DefaultPassMarks), "Pass marks cannot exceed max marks.");
        if (await _db.Subjects.AnyAsync(x => x.SchoolId == schoolId && x.Id != model.Id && (x.Code == model.Code.Trim() || x.Title == model.Title.Trim())))
            ModelState.AddModelError(nameof(model.Code), "Subject code or title already exists.");
        if (!model.HasTheory && !model.HasPractical)
            ModelState.AddModelError(nameof(model.HasTheory), "A subject must have theory, practical, or both.");
        if (!ModelState.IsValid) return View(model);

        item.Code = model.Code.Trim();
        item.Title = model.Title.Trim();
        item.HasTheory = model.HasTheory;
        item.HasPractical = model.HasPractical;
        item.DefaultMaxMarks = model.DefaultMaxMarks;
        item.DefaultPassMarks = model.DefaultPassMarks;
        item.IsActive = model.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.SubjectUpdated", "Subject", item.Id.ToString(), item.Code);
        TempData["Success"] = "Subject updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubject(ClassSubjectFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        if (!await _db.AcademicSessions.AnyAsync(x => x.Id == model.AcademicSessionId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.AcademicSessionId), "Select a valid academic session.");
        if (!await _db.SchoolClasses.AnyAsync(x => x.Id == model.SchoolClassId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.SchoolClassId), "Select a valid class.");
        if (!await _db.Subjects.AnyAsync(x => x.Id == model.SubjectId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.SubjectId), "Select a valid subject.");
        if (model.PassMarks.HasValue && model.MaxMarks.HasValue && model.PassMarks > model.MaxMarks)
            ModelState.AddModelError(nameof(model.PassMarks), "Pass marks cannot exceed max marks.");
        if (await _db.ClassSubjects.AnyAsync(x => x.AcademicSessionId == model.AcademicSessionId && x.SchoolClassId == model.SchoolClassId && x.SubjectId == model.SubjectId))
            ModelState.AddModelError(string.Empty, "This subject is already mapped to the class for this session.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Index), new { sessionId = model.AcademicSessionId });
        }

        var entity = new ClassSubject
        {
            SchoolId = schoolId.Value, AcademicSessionId = model.AcademicSessionId, SchoolClassId = model.SchoolClassId,
            SubjectId = model.SubjectId, MaxMarks = model.MaxMarks, PassMarks = model.PassMarks
        };
        _db.ClassSubjects.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.ClassSubjectAssigned", "ClassSubject", entity.Id.ToString());
        TempData["Success"] = "Subject assigned to class.";
        return RedirectToAction(nameof(Index), new { sessionId = model.AcademicSessionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveClassSubject(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.ClassSubjects.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        var sessionId = item.AcademicSessionId;
        _db.ClassSubjects.Remove(item);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.ClassSubjectRemoved", "ClassSubject", id.ToString());
        TempData["Success"] = "Class-subject mapping removed.";
        return RedirectToAction(nameof(Index), new { sessionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTeacher(TeacherAssignmentFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        if (!await _db.AcademicSessions.AnyAsync(x => x.Id == model.AcademicSessionId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.AcademicSessionId), "Select a valid academic session.");
        if (!await _db.SchoolClasses.AnyAsync(x => x.Id == model.SchoolClassId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.SchoolClassId), "Select a valid class.");
        if (model.SectionId.HasValue && !await _db.Sections.AnyAsync(x => x.Id == model.SectionId.Value && x.SchoolId == schoolId.Value && x.SchoolClassId == model.SchoolClassId))
            ModelState.AddModelError(nameof(model.SectionId), "Section must belong to the selected class.");
        if (!await _db.Subjects.AnyAsync(x => x.Id == model.SubjectId && x.SchoolId == schoolId.Value))
            ModelState.AddModelError(nameof(model.SubjectId), "Select a valid subject.");
        if (!await _db.ClassSubjects.AnyAsync(x => x.SchoolId == schoolId.Value && x.AcademicSessionId == model.AcademicSessionId && x.SchoolClassId == model.SchoolClassId && x.SubjectId == model.SubjectId && x.IsActive))
            ModelState.AddModelError(nameof(model.SubjectId), "Map this subject to the selected class/session before assigning a teacher.");
        if (!await IsSchoolTeacherAsync(model.TeacherUserId, schoolId.Value))
            ModelState.AddModelError(nameof(model.TeacherUserId), "Select a valid teacher from this school.");
        if (await _db.TeacherAssignments.AnyAsync(x => x.SchoolId == schoolId.Value && x.AcademicSessionId == model.AcademicSessionId && x.SchoolClassId == model.SchoolClassId && x.SectionId == model.SectionId && x.SubjectId == model.SubjectId && x.TeacherUserId == model.TeacherUserId && x.IsActive))
            ModelState.AddModelError(string.Empty, "This teacher assignment already exists.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Index), new { sessionId = model.AcademicSessionId });
        }

        var entity = new TeacherAssignment
        {
            SchoolId = schoolId.Value, AcademicSessionId = model.AcademicSessionId, SchoolClassId = model.SchoolClassId,
            SectionId = model.SectionId, SubjectId = model.SubjectId, TeacherUserId = model.TeacherUserId,
            Notes = NullIfBlank(model.Notes)
        };
        _db.TeacherAssignments.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.TeacherAssigned", "TeacherAssignment", entity.Id.ToString());
        TempData["Success"] = "Teacher assigned.";
        return RedirectToAction(nameof(Index), new { sessionId = model.AcademicSessionId });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeacherAssignment(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        var item = await _db.TeacherAssignments.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId);
        if (item is null) return NotFound();
        item.IsActive = false;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.TeacherAssignmentDisabled", "TeacherAssignment", id.ToString());
        TempData["Success"] = "Teacher assignment disabled.";
        return RedirectToAction(nameof(Index), new { sessionId = item.AcademicSessionId });
    }

    [HttpGet]
    public async Task<IActionResult> Roster(int classId, int? sectionId, int? sessionId)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        var cls = await _db.SchoolClasses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == classId && x.SchoolId == schoolId.Value);
        if (cls is null) return NotFound();
        var section = sectionId.HasValue
            ? await _db.Sections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sectionId.Value && x.SchoolId == schoolId.Value && x.SchoolClassId == classId)
            : null;
        if (sectionId.HasValue && section is null) return NotFound();

        var session = sessionId.HasValue
            ? await _db.AcademicSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId.Value && x.SchoolId == schoolId.Value)
            : await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == schoolId.Value && x.Status == AcademicSessionStatus.Active).FirstOrDefaultAsync();
        if (session is null) return NotFound();

        if (User.IsInRole(AppRoles.Teacher) && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Principal) && !User.IsInRole(AppRoles.SuperAdmin))
        {
            var userId = _userManager.GetUserId(User);
            bool allowed;
            if (sectionId.HasValue)
            {
                allowed = await _db.TeacherAssignments.AnyAsync(x =>
                    x.SchoolId == schoolId.Value && x.AcademicSessionId == session.Id && x.SchoolClassId == classId &&
                    x.IsActive && x.TeacherUserId == userId && (x.SectionId == null || x.SectionId == sectionId.Value));
                if (!allowed)
                {
                    allowed = await _db.Sections.AnyAsync(x =>
                        x.SchoolId == schoolId.Value && x.Id == sectionId.Value && x.ClassTeacherUserId == userId);
                }
            }
            else
            {
                allowed = await _db.TeacherAssignments.AnyAsync(x =>
                    x.SchoolId == schoolId.Value && x.AcademicSessionId == session.Id && x.SchoolClassId == classId &&
                    x.IsActive && x.TeacherUserId == userId && x.SectionId == null);
            }
            if (!allowed) return Forbid();
        }

        var query = _db.StudentEnrollments.AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId.Value && x.AcademicSessionId == session.Id && x.SchoolClassId == classId && x.IsCurrent);
        if (sectionId.HasValue) query = query.Where(x => x.SectionId == sectionId.Value);

        return View(new ClassRosterViewModel
        {
            SchoolClass = cls,
            Section = section,
            AcademicSession = session,
            Enrollments = await query.OrderBy(x => x.RollNumber).ThenBy(x => x.Student.FullName).ToListAsync()
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MapLegacyPlacements()
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        var classes = await _db.SchoolClasses.Include(x => x.Sections).Include(x => x.Groups).Where(x => x.SchoolId == schoolId.Value).ToListAsync();
        var rows = await _db.StudentEnrollments.Where(x => x.SchoolId == schoolId.Value && x.SchoolClassId == null).ToListAsync();
        var mapped = 0;
        foreach (var row in rows)
        {
            var cls = classes.FirstOrDefault(x => string.Equals(x.Name.Trim(), row.ClassName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (cls is null) continue;
            row.SchoolClassId = cls.Id;
            if (!string.IsNullOrWhiteSpace(row.SectionName))
                row.SectionId = cls.Sections.FirstOrDefault(x => string.Equals(x.Name.Trim(), row.SectionName.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
            if (!string.IsNullOrWhiteSpace(row.GroupStream))
                row.AcademicGroupId = cls.Groups.FirstOrDefault(x => string.Equals(x.Name.Trim(), row.GroupStream.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
            row.UpdatedAtUtc = DateTime.UtcNow;
            mapped++;
        }
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AcademicStructure.LegacyPlacementsMapped", "StudentEnrollment", null, $"Mapped={mapped}; TotalUnmappedBefore={rows.Count}");
        TempData["Success"] = $"Mapped {mapped} legacy placement record(s). Unmatched names were left unchanged.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> RequireSchoolIdAsync()
    {
        var id = await _schoolContext.GetCurrentSchoolIdAsync();
        if (id.HasValue) return id;
        return (await _schoolContext.GetCurrentSchoolAsync())?.Id;
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Info"] = "Configure the School Profile before using Classes & Subjects.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private async Task<bool> IsSchoolTeacherAsync(string userId, int schoolId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is not null && user.SchoolId == schoolId && user.IsActive && await _userManager.IsInRoleAsync(user, AppRoles.Teacher);
    }

    private async Task LoadEditListsAsync(int schoolId, int? selectedClassId, string? selectedTeacherId)
    {
        ViewBag.Classes = await _db.SchoolClasses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        var teachers = await _userManager.GetUsersInRoleAsync(AppRoles.Teacher);
        ViewBag.Teachers = teachers.Where(x => x.SchoolId == schoolId && x.IsActive).OrderBy(x => x.FullName).ToList();
        ViewBag.SelectedClassId = selectedClassId;
        ViewBag.SelectedTeacherId = selectedTeacherId;
    }

    private string FirstError()
        => ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Please correct the form and try again.";

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
