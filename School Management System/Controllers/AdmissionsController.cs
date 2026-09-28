using System.Security.Claims;
using System.Text;
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
public class AdmissionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IAdmissionFileService _fileService;
    private readonly IAdmissionService _admissionService;
    private readonly IAuditService _audit;

    public AdmissionsController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IAdmissionFileService fileService,
        IAdmissionService admissionService,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _fileService = fileService;
        _admissionService = admissionService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, AdmissionEnquiryStage? stage, string? desiredClass)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var baseQuery = _db.AdmissionEnquiries
            .AsNoTracking()
            .Include(x => x.AcademicSession)
            .Where(x => x.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            baseQuery = baseQuery.Where(x =>
                x.StudentName.Contains(term) ||
                x.ParentGuardianName.Contains(term) ||
                x.ContactNumber.Contains(term));
        }

        if (stage.HasValue)
            baseQuery = baseQuery.Where(x => x.Stage == stage.Value);

        if (!string.IsNullOrWhiteSpace(desiredClass))
        {
            var cls = desiredClass.Trim();
            baseQuery = baseQuery.Where(x => x.DesiredClass == cls);
        }

        var enquiries = await baseQuery
            .OrderBy(x => x.FollowUpDate ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(300)
            .ToListAsync();

        var allForStats = _db.AdmissionEnquiries.AsNoTracking().Where(x => x.SchoolId == schoolId.Value);
        var today = DateTime.Today;

        var classStats = await allForStats
            .GroupBy(x => x.DesiredClass)
            .Select(g => new AdmissionClassStatViewModel
            {
                DesiredClass = g.Key,
                EnquiryCount = g.Count(),
                AdmittedCount = g.Count(x => x.Stage == AdmissionEnquiryStage.Admitted)
            })
            .OrderBy(x => x.DesiredClass)
            .ToListAsync();

        var model = new AdmissionsIndexViewModel
        {
            Enquiries = enquiries,
            ClassStats = classStats,
            Query = q,
            Stage = stage,
            DesiredClass = desiredClass,
            TotalEnquiries = await allForStats.CountAsync(),
            Approved = await allForStats.CountAsync(x => x.Stage == AdmissionEnquiryStage.Approved),
            Admitted = await allForStats.CountAsync(x => x.Stage == AdmissionEnquiryStage.Admitted),
            FollowUpsDue = await allForStats.CountAsync(x =>
                x.FollowUpDate.HasValue && x.FollowUpDate.Value.Date <= today &&
                x.Stage != AdmissionEnquiryStage.Rejected && x.Stage != AdmissionEnquiryStage.Admitted)
        };

        ViewBag.ClassFilter = await _db.AdmissionEnquiries.AsNoTracking()
            .Where(x => x.SchoolId == schoolId.Value)
            .Select(x => x.DesiredClass)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateEnquiry()
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();
        await LoadSessionsAsync(schoolId.Value, null);
        return View(new AdmissionEnquiryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEnquiry(AdmissionEnquiryFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        await ValidateEnquiryAsync(model, schoolId.Value, null);
        if (!ModelState.IsValid)
        {
            await LoadSessionsAsync(schoolId.Value, model.AcademicSessionId);
            return View(model);
        }

        var enquiry = new AdmissionEnquiry
        {
            SchoolId = schoolId.Value,
            AcademicSessionId = model.AcademicSessionId,
            StudentName = model.StudentName.Trim(),
            ParentGuardianName = model.ParentGuardianName.Trim(),
            ContactNumber = model.ContactNumber.Trim(),
            Email = model.Email?.Trim(),
            DesiredClass = model.DesiredClass.Trim(),
            SourceReferral = model.SourceReferral?.Trim(),
            FollowUpDate = model.FollowUpDate?.Date,
            Stage = model.Stage,
            Notes = model.Notes?.Trim()
        };

        _db.AdmissionEnquiries.Add(enquiry);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AdmissionEnquiry.Created", "AdmissionEnquiry", enquiry.Id.ToString(), $"Student={enquiry.StudentName}; Stage={enquiry.Stage}");

        TempData["Success"] = "Admission enquiry created successfully.";
        return RedirectToAction(nameof(EnquiryDetails), new { id = enquiry.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditEnquiry(int id)
    {
        var enquiry = await FindOwnedEnquiryAsync(id);
        if (enquiry is null) return NotFound();
        if (enquiry.Stage == AdmissionEnquiryStage.Admitted) return Forbid();

        var model = new AdmissionEnquiryFormViewModel
        {
            Id = enquiry.Id,
            AcademicSessionId = enquiry.AcademicSessionId,
            StudentName = enquiry.StudentName,
            ParentGuardianName = enquiry.ParentGuardianName,
            ContactNumber = enquiry.ContactNumber,
            Email = enquiry.Email,
            DesiredClass = enquiry.DesiredClass,
            SourceReferral = enquiry.SourceReferral,
            FollowUpDate = enquiry.FollowUpDate,
            Stage = enquiry.Stage,
            Notes = enquiry.Notes
        };

        await LoadSessionsAsync(enquiry.SchoolId, model.AcademicSessionId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEnquiry(AdmissionEnquiryFormViewModel model)
    {
        var enquiry = await FindOwnedEnquiryAsync(model.Id);
        if (enquiry is null) return NotFound();
        if (enquiry.Stage == AdmissionEnquiryStage.Admitted) return Forbid();

        await ValidateEnquiryAsync(model, enquiry.SchoolId, enquiry);
        if (!ModelState.IsValid)
        {
            await LoadSessionsAsync(enquiry.SchoolId, model.AcademicSessionId);
            return View(model);
        }

        var oldStage = enquiry.Stage;
        enquiry.AcademicSessionId = model.AcademicSessionId;
        enquiry.StudentName = model.StudentName.Trim();
        enquiry.ParentGuardianName = model.ParentGuardianName.Trim();
        enquiry.ContactNumber = model.ContactNumber.Trim();
        enquiry.Email = model.Email?.Trim();
        enquiry.DesiredClass = model.DesiredClass.Trim();
        enquiry.SourceReferral = model.SourceReferral?.Trim();
        enquiry.FollowUpDate = model.FollowUpDate?.Date;
        enquiry.Stage = model.Stage;
        enquiry.Notes = model.Notes?.Trim();
        enquiry.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AdmissionEnquiry.Updated", "AdmissionEnquiry", enquiry.Id.ToString(), $"Stage={oldStage}->{enquiry.Stage}; Student={enquiry.StudentName}");

        TempData["Success"] = "Admission enquiry updated.";
        return RedirectToAction(nameof(EnquiryDetails), new { id = enquiry.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EnquiryDetails(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var enquiry = await _db.AdmissionEnquiries
            .AsNoTracking()
            .Include(x => x.AcademicSession)
            .Include(x => x.AdmissionApplication)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value);

        return enquiry is null ? NotFound() : View(enquiry);
    }

    [HttpGet]
    public async Task<IActionResult> StartAdmission(int? enquiryId)
    {
        if (!enquiryId.HasValue)
        {
            var schoolId = await RequireSchoolIdAsync();
            if (!schoolId.HasValue) return RedirectToSchoolSetup();
            var directModel = new AdmissionApplicationFormViewModel
            {
                AcademicSessionId = await GetActiveSessionIdAsync(schoolId.Value) ?? 0
            };
            await LoadSessionsAsync(schoolId.Value, directModel.AcademicSessionId);
            return View("ApplicationForm", directModel);
        }
        var enquiry = await FindOwnedEnquiryAsync(enquiryId.Value);
        if (enquiry is null) return NotFound();
        if (enquiry.Stage != AdmissionEnquiryStage.Approved)
        {
            TempData["Error"] = "Approve the enquiry before starting the admission form.";
            return RedirectToAction(nameof(EnquiryDetails), new { id = enquiryId });
        }

        var existing = await _db.AdmissionApplications.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AdmissionEnquiryId == enquiryId);
        if (existing is not null)
            return RedirectToAction(nameof(ApplicationDetails), new { id = existing.Id });

        var sessionId = enquiry.AcademicSessionId ?? await GetActiveSessionIdAsync(enquiry.SchoolId);
        if (!sessionId.HasValue)
        {
            TempData["Error"] = "Create or activate an academic session before starting admission.";
            return RedirectToAction(nameof(EnquiryDetails), new { id = enquiryId });
        }

        var model = new AdmissionApplicationFormViewModel
        {
            AdmissionEnquiryId = enquiry.Id,
            AcademicSessionId = sessionId.Value,
            StudentName = enquiry.StudentName,
            DesiredClass = enquiry.DesiredClass,
            GuardianName = enquiry.ParentGuardianName,
            GuardianPhone = enquiry.ContactNumber,
            GuardianRelationship = "Parent / Guardian",
            AdmissionDate = DateTime.Today,
            DateOfBirth = default
        };

        await LoadSessionsAsync(enquiry.SchoolId, model.AcademicSessionId);
        return View("ApplicationForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartAdmission(AdmissionApplicationFormViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        AdmissionEnquiry? enquiry = null;
        if (model.AdmissionEnquiryId.HasValue)
        {
            enquiry = await _db.AdmissionEnquiries
                .FirstOrDefaultAsync(x => x.Id == model.AdmissionEnquiryId.Value && x.SchoolId == schoolId.Value);
            if (enquiry is null) return NotFound();
            if (enquiry.Stage != AdmissionEnquiryStage.Approved)
                ModelState.AddModelError(string.Empty, "The enquiry must be Approved before admission can start.");
            if (await _db.AdmissionApplications.AnyAsync(x => x.AdmissionEnquiryId == enquiry.Id))
                ModelState.AddModelError(string.Empty, "An admission application already exists for this enquiry.");
        }

        await ValidateApplicationAsync(model, schoolId.Value);
        if (!ModelState.IsValid)
        {
            await LoadSessionsAsync(schoolId.Value, model.AcademicSessionId);
            return View("ApplicationForm", model);
        }

        var application = BuildApplication(model, schoolId.Value);
        application.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _db.AdmissionApplications.Add(application);
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("AdmissionApplication.Created", "AdmissionApplication", application.Id.ToString(), $"Student={application.StudentName}; DesiredClass={application.DesiredClass}");
        TempData["Success"] = "Admission form created. Add documents, review it, then submit.";
        return RedirectToAction(nameof(ApplicationDetails), new { id = application.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Applications(string? q, AdmissionApplicationStatus? status)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var query = _db.AdmissionApplications.AsNoTracking()
            .Include(x => x.AcademicSession)
            .Where(x => x.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.StudentName.Contains(term) ||
                                     (x.AdmissionNumber != null && x.AdmissionNumber.Contains(term)) ||
                                     x.GuardianPhone.Contains(term));
        }
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var model = new AdmissionApplicationsIndexViewModel
        {
            Query = q,
            Status = status,
            Applications = await query.OrderByDescending(x => x.CreatedAtUtc).Take(300).ToListAsync()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditApplication(int id)
    {
        var application = await FindOwnedApplicationAsync(id);
        if (application is null) return NotFound();
        if (application.Status != AdmissionApplicationStatus.Draft)
        {
            TempData["Error"] = "Only Draft admission applications can be edited.";
            return RedirectToAction(nameof(ApplicationDetails), new { id });
        }

        await LoadSessionsAsync(application.SchoolId, application.AcademicSessionId);
        return View("ApplicationForm", ToForm(application));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditApplication(AdmissionApplicationFormViewModel model)
    {
        var application = await FindOwnedApplicationAsync(model.Id);
        if (application is null) return NotFound();
        if (application.Status != AdmissionApplicationStatus.Draft) return Forbid();

        await ValidateApplicationAsync(model, application.SchoolId);
        if (!ModelState.IsValid)
        {
            await LoadSessionsAsync(application.SchoolId, model.AcademicSessionId);
            return View("ApplicationForm", model);
        }

        ApplyForm(application, model);
        application.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AdmissionApplication.Updated", "AdmissionApplication", application.Id.ToString(), application.StudentName);

        TempData["Success"] = "Admission form updated.";
        return RedirectToAction(nameof(ApplicationDetails), new { id = application.Id });
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationDetails(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var application = await _db.AdmissionApplications.AsNoTracking()
            .Include(x => x.AcademicSession)
            .Include(x => x.AdmissionEnquiry)
            .Include(x => x.Documents)
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value);

        if (application is null) return NotFound();
        ViewBag.UploadModel = new AdmissionDocumentUploadViewModel { AdmissionApplicationId = application.Id };
        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitApplication(int id)
    {
        var application = await FindOwnedApplicationAsync(id);
        if (application is null) return NotFound();
        if (application.Status != AdmissionApplicationStatus.Draft) return Forbid();

        application.Status = AdmissionApplicationStatus.Submitted;
        application.SubmittedAtUtc = DateTime.UtcNow;
        application.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AdmissionApplication.Submitted", "AdmissionApplication", id.ToString(), application.StudentName);

        TempData["Success"] = "Admission application submitted for final approval.";
        return RedirectToAction(nameof(ApplicationDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectApplication(int id)
    {
        var application = await FindOwnedApplicationAsync(id);
        if (application is null) return NotFound();
        if (application.Status == AdmissionApplicationStatus.Admitted) return Forbid();

        application.Status = AdmissionApplicationStatus.Rejected;
        application.UpdatedAtUtc = DateTime.UtcNow;
        if (application.AdmissionEnquiryId.HasValue)
        {
            var enquiry = await _db.AdmissionEnquiries.FirstOrDefaultAsync(x => x.Id == application.AdmissionEnquiryId.Value);
            if (enquiry is not null)
            {
                enquiry.Stage = AdmissionEnquiryStage.Rejected;
                enquiry.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AdmissionApplication.Rejected", "AdmissionApplication", id.ToString(), application.StudentName);
        TempData["Success"] = "Admission application rejected.";
        return RedirectToAction(nameof(ApplicationDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(AdmissionDocumentUploadViewModel model)
    {
        var application = await FindOwnedApplicationAsync(model.AdmissionApplicationId);
        if (application is null) return NotFound();
        if (application.Status != AdmissionApplicationStatus.Draft)
        {
            TempData["Error"] = "Documents can only be added while the admission application is Draft.";
            return RedirectToAction(nameof(ApplicationDetails), new { id = model.AdmissionApplicationId });
        }

        if (model.File is null)
        {
            TempData["Error"] = "Choose a document to upload.";
            return RedirectToAction(nameof(ApplicationDetails), new { id = model.AdmissionApplicationId });
        }

        var saved = await _fileService.SaveAsync(model.File, application.SchoolId, application.Id, HttpContext.RequestAborted);
        if (!saved.Success || saved.StorageKey is null)
        {
            TempData["Error"] = saved.Message;
            return RedirectToAction(nameof(ApplicationDetails), new { id = application.Id });
        }

        var document = new AdmissionDocument
        {
            AdmissionApplicationId = application.Id,
            DocumentType = model.DocumentType,
            OriginalFileName = Path.GetFileName(model.File.FileName),
            StorageKey = saved.StorageKey,
            ContentType = saved.ContentType ?? "application/octet-stream",
            SizeBytes = saved.SizeBytes
        };
        _db.AdmissionDocuments.Add(document);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync("AdmissionDocument.Uploaded", "AdmissionApplication", application.Id.ToString(), $"Type={document.DocumentType}; File={document.OriginalFileName}");

        TempData["Success"] = "Document uploaded.";
        return RedirectToAction(nameof(ApplicationDetails), new { id = application.Id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var document = await _db.AdmissionDocuments.AsNoTracking()
            .Include(x => x.AdmissionApplication)
            .FirstOrDefaultAsync(x => x.Id == id && x.AdmissionApplication.SchoolId == schoolId.Value);
        if (document is null) return NotFound();

        var stream = await _fileService.OpenReadAsync(document.StorageKey, HttpContext.RequestAborted);
        if (stream is null) return NotFound();
        return File(stream, document.ContentType, document.OriginalFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Admit(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var result = await _admissionService.AdmitAsync(
            id,
            schoolId.Value,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            HttpContext.RequestAborted);

        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (result.Success)
            await _audit.WriteAsync("Student.Admitted", "AdmissionApplication", id.ToString(), $"StudentId={result.StudentId}; AdmissionNo={result.AdmissionNumber}");

        return RedirectToAction(nameof(ApplicationDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ExportAdmissionsCsv()
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToSchoolSetup();

        var admitted = await _db.AdmissionApplications.AsNoTracking()
            .Include(x => x.AcademicSession)
            .Where(x => x.SchoolId == schoolId.Value && x.Status == AdmissionApplicationStatus.Admitted)
            .OrderBy(x => x.AdmissionNumber)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Admission Number,Student Name,Guardian,Guardian Phone,Desired Class,Academic Session,Admission Date,B-Form/CNIC");
        foreach (var item in admitted)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Csv(item.AdmissionNumber), Csv(item.StudentName), Csv(item.GuardianName), Csv(item.GuardianPhone),
                Csv(item.DesiredClass), Csv(item.AcademicSession.Name), Csv(item.AdmissionDate.ToString("yyyy-MM-dd")), Csv(item.BFormCnic)
            }));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"admission-register-{DateTime.Today:yyyyMMdd}.csv");
    }

    private async Task ValidateEnquiryAsync(AdmissionEnquiryFormViewModel model, int schoolId, AdmissionEnquiry? existing)
    {
        if (model.Stage == AdmissionEnquiryStage.Admitted)
            ModelState.AddModelError(nameof(model.Stage), "Admitted status is set automatically after final admission.");

        if (existing?.AdmissionApplication is not null && model.Stage == AdmissionEnquiryStage.Rejected)
            ModelState.AddModelError(nameof(model.Stage), "Reject the admission application from its details page.");

        if (model.AcademicSessionId.HasValue && !await _db.AcademicSessions.AnyAsync(x =>
                x.Id == model.AcademicSessionId.Value && x.SchoolId == schoolId && x.Status != AcademicSessionStatus.Archived))
            ModelState.AddModelError(nameof(model.AcademicSessionId), "Select a valid academic session for this school.");
    }

    private async Task ValidateApplicationAsync(AdmissionApplicationFormViewModel model, int schoolId)
    {
        var validSession = await _db.AcademicSessions.AnyAsync(x =>
            x.Id == model.AcademicSessionId && x.SchoolId == schoolId &&
            (x.Status == AcademicSessionStatus.Active || x.Status == AcademicSessionStatus.Draft));
        if (!validSession)
            ModelState.AddModelError(nameof(model.AcademicSessionId), "Admissions are allowed only for Draft or Active sessions.");

        if (model.DateOfBirth.Date >= model.AdmissionDate.Date)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth must be before the admission date.");
        if (model.DateOfBirth == default)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Enter the student's date of birth.");
        if (model.AdmissionDate == default)
            ModelState.AddModelError(nameof(model.AdmissionDate), "Choose an admission date.");
        if (!await _db.SchoolClasses.AnyAsync(x => x.SchoolId == schoolId && x.IsActive && x.Name == model.DesiredClass))
            ModelState.AddModelError(nameof(model.DesiredClass), "Choose an active school class. If it is missing, ask your administrator to add it in school setup.");
    }

    private AdmissionApplication BuildApplication(AdmissionApplicationFormViewModel model, int schoolId)
    {
        var entity = new AdmissionApplication
        {
            SchoolId = schoolId,
            AdmissionEnquiryId = model.AdmissionEnquiryId
        };
        ApplyForm(entity, model);
        return entity;
    }

    private static void ApplyForm(AdmissionApplication entity, AdmissionApplicationFormViewModel model)
    {
        entity.AcademicSessionId = model.AcademicSessionId;
        entity.StudentName = model.StudentName.Trim();
        entity.Gender = model.Gender?.Trim();
        entity.DateOfBirth = model.DateOfBirth.Date;
        entity.BFormCnic = model.BFormCnic?.Trim();
        entity.Address = model.Address?.Trim();
        entity.StudentContactNumber = model.StudentContactNumber?.Trim();
        entity.DesiredClass = model.DesiredClass.Trim();
        entity.AdmissionDate = model.AdmissionDate.Date;
        entity.PreviousSchoolName = model.PreviousSchoolName?.Trim();
        entity.PreviousClass = model.PreviousClass?.Trim();
        entity.PreviousResultSummary = model.PreviousResultSummary?.Trim();
        entity.GuardianName = model.GuardianName.Trim();
        entity.GuardianRelationship = model.GuardianRelationship.Trim();
        entity.GuardianPhone = model.GuardianPhone.Trim();
        entity.GuardianOccupation = model.GuardianOccupation?.Trim();
        entity.GuardianCnic = model.GuardianCnic?.Trim();
        entity.GuardianAddress = model.GuardianAddress?.Trim();
        entity.EmergencyContactName = model.EmergencyContactName?.Trim();
        entity.EmergencyContactRelationship = model.EmergencyContactRelationship?.Trim();
        entity.EmergencyContactPhone = model.EmergencyContactPhone?.Trim();
        entity.Notes = model.Notes?.Trim();
    }

    private static AdmissionApplicationFormViewModel ToForm(AdmissionApplication entity) => new()
    {
        Id = entity.Id,
        AdmissionEnquiryId = entity.AdmissionEnquiryId,
        AcademicSessionId = entity.AcademicSessionId,
        StudentName = entity.StudentName,
        Gender = entity.Gender,
        DateOfBirth = entity.DateOfBirth,
        BFormCnic = entity.BFormCnic,
        Address = entity.Address,
        StudentContactNumber = entity.StudentContactNumber,
        DesiredClass = entity.DesiredClass,
        AdmissionDate = entity.AdmissionDate,
        PreviousSchoolName = entity.PreviousSchoolName,
        PreviousClass = entity.PreviousClass,
        PreviousResultSummary = entity.PreviousResultSummary,
        GuardianName = entity.GuardianName,
        GuardianRelationship = entity.GuardianRelationship,
        GuardianPhone = entity.GuardianPhone,
        GuardianOccupation = entity.GuardianOccupation,
        GuardianCnic = entity.GuardianCnic,
        GuardianAddress = entity.GuardianAddress,
        EmergencyContactName = entity.EmergencyContactName,
        EmergencyContactRelationship = entity.EmergencyContactRelationship,
        EmergencyContactPhone = entity.EmergencyContactPhone,
        Notes = entity.Notes
    };

    private async Task<AdmissionEnquiry?> FindOwnedEnquiryAsync(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return null;
        return await _db.AdmissionEnquiries
            .Include(x => x.AdmissionApplication)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value);
    }

    private async Task<AdmissionApplication?> FindOwnedApplicationAsync(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return null;
        return await _db.AdmissionApplications.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value);
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
        TempData["Info"] = "Configure the School Profile before using Admissions.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private async Task<int?> GetActiveSessionIdAsync(int schoolId)
    {
        return await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }

    private async Task LoadSessionsAsync(int schoolId, int? selected)
    {
        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status != AcademicSessionStatus.Archived)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
        ViewBag.AcademicSessions = new SelectList(sessions, "Id", "Name", selected);
        ViewBag.AdmissionSessions = new SelectList(sessions.Where(x => x.Status == AcademicSessionStatus.Active || x.Status == AcademicSessionStatus.Draft), "Id", "Name", selected);
        ViewBag.SchoolClasses = await _db.SchoolClasses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).OrderBy(x => x.SortOrder).Select(x => x.Name).ToListAsync();
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return '"' + text.Replace("\"", "\"\"") + '"';
    }
}
