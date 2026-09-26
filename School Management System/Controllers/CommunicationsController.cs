using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize]
public class CommunicationsController : Controller
{
    private const string ManageRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Receptionist + "," + AppRoles.ExamController;

    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly ICommunicationFileService _files;
    private readonly ICommunicationDispatcher _dispatcher;
    private readonly IAuditService _audit;

    public CommunicationsController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        ICommunicationFileService files,
        ICommunicationDispatcher dispatcher,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _files = files;
        _dispatcher = dispatcher;
        _audit = audit;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        var now = DateTime.UtcNow;
        var audiences = GetVisibleAudiences();
        var canManage = User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin)
                        || User.IsInRole(AppRoles.Receptionist) || User.IsInRole(AppRoles.ExamController);

        var notices = await _db.Notices.AsNoTracking()
            .Include(x => x.SchoolClass).Include(x => x.Section)
            .Where(x => x.SchoolId == schoolId.Value && x.IsPublished && !x.IsArchived
                        && (!x.PublishFromUtc.HasValue || x.PublishFromUtc <= now)
                        && (!x.PublishUntilUtc.HasValue || x.PublishUntilUtc >= now)
                        && (canManage || audiences.Contains(x.Audience)))
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var documents = await _db.SchoolDocuments.AsNoTracking()
            .Include(x => x.SchoolClass).Include(x => x.Section)
            .Where(x => x.SchoolId == schoolId.Value && x.IsActive
                        && (!x.EffectiveFromUtc.HasValue || x.EffectiveFromUtc <= now)
                        && (!x.EffectiveUntilUtc.HasValue || x.EffectiveUntilUtc >= now)
                        && (canManage || audiences.Contains(x.Audience)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return View(new CommunicationIndexViewModel
        {
            Notices = notices,
            Documents = documents,
            CanManage = canManage
        });
    }

    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> Manage(CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        var notices = await _db.Notices.AsNoTracking().Include(x => x.SchoolClass).Include(x => x.Section)
            .Where(x => x.SchoolId == schoolId.Value)
            .OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);

        var documents = await _db.SchoolDocuments.AsNoTracking().Include(x => x.SchoolClass).Include(x => x.Section)
            .Where(x => x.SchoolId == schoolId.Value)
            .OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);

        var history = await _db.CommunicationHistory.AsNoTracking().Include(x => x.Notice)
            .Where(x => x.SchoolId == schoolId.Value)
            .OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);

        return View(new CommunicationManageViewModel
        {
            Notices = notices,
            Documents = documents,
            History = history,
            PublishedNoticeCount = notices.Count(x => x.IsPublished && !x.IsArchived),
            ActiveDocumentCount = documents.Count(x => x.IsActive),
            ExternalNotConfiguredCount = history.Count(x => x.Status == CommunicationDeliveryStatus.NotConfigured)
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> CreateNotice(CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        var model = new NoticeFormViewModel();
        await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
        return View("NoticeForm", model);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> EditNotice(int id, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        var item = await _db.Notices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null) return NotFound();

        var model = new NoticeFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Body = item.Body,
            Audience = item.Audience,
            SchoolClassId = item.SchoolClassId,
            SectionId = item.SectionId,
            PublishFromLocal = item.PublishFromUtc?.ToLocalTime(),
            PublishUntilLocal = item.PublishUntilUtc?.ToLocalTime(),
            ExistingAttachmentName = item.AttachmentOriginalName,
            RowVersion = Convert.ToBase64String(item.RowVersion ?? [])
        };
        await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
        return View("NoticeForm", model);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotice(NoticeFormViewModel model, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        var user = await _schoolContext.GetCurrentUserAsync();
        if (!schoolId.HasValue || user is null) return RedirectToAction("Index", "SchoolSetup");

        ValidateAudienceTarget(model.Audience, model.SchoolClassId, model.SectionId);
        if (model.PublishFromLocal.HasValue && model.PublishUntilLocal.HasValue && model.PublishUntilLocal <= model.PublishFromLocal)
            ModelState.AddModelError(nameof(model.PublishUntilLocal), "Visible-until must be later than visible-from.");

        if (!ModelState.IsValid)
        {
            await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
            return View("NoticeForm", model);
        }

        Notice entity;
        var isNew = !model.Id.HasValue;
        if (isNew)
        {
            entity = new Notice { SchoolId = schoolId.Value, CreatedByUserId = user.Id };
            _db.Notices.Add(entity);
        }
        else
        {
            entity = await _db.Notices.FirstOrDefaultAsync(x => x.Id == model.Id!.Value && x.SchoolId == schoolId.Value, cancellationToken)
                     ?? throw new InvalidOperationException("Notice not found.");
            ApplyRowVersion(entity, model.RowVersion);
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        entity.Title = model.Title.Trim();
        entity.Body = model.Body.Trim();
        entity.Audience = model.Audience;
        entity.SchoolClassId = model.Audience == NoticeAudience.Class ? model.SchoolClassId : null;
        entity.SectionId = model.Audience == NoticeAudience.Class ? model.SectionId : null;
        entity.PublishFromUtc = model.PublishFromLocal?.ToUniversalTime();
        entity.PublishUntilUtc = model.PublishUntilLocal?.ToUniversalTime();

        if (model.RemoveExistingAttachment && !string.IsNullOrWhiteSpace(entity.AttachmentStorageKey))
        {
            await _files.DeleteAsync(entity.AttachmentStorageKey, cancellationToken);
            entity.AttachmentOriginalName = null;
            entity.AttachmentStorageKey = null;
            entity.AttachmentContentType = null;
            entity.AttachmentSizeBytes = null;
        }

        if (model.Attachment is not null)
        {
            var saved = await _files.SaveAsync(model.Attachment, schoolId.Value, "notice", cancellationToken);
            if (!saved.Success)
            {
                ModelState.AddModelError(nameof(model.Attachment), saved.Message);
                await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
                return View("NoticeForm", model);
            }

            if (!string.IsNullOrWhiteSpace(entity.AttachmentStorageKey))
                await _files.DeleteAsync(entity.AttachmentStorageKey, cancellationToken);

            entity.AttachmentOriginalName = Path.GetFileName(model.Attachment.FileName);
            entity.AttachmentStorageKey = saved.StorageKey;
            entity.AttachmentContentType = saved.ContentType;
            entity.AttachmentSizeBytes = saved.SizeBytes;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(isNew ? "Notice.Create" : "Notice.Update", nameof(Notice), entity.Id.ToString(), $"Title={entity.Title}; Audience={entity.Audience}");
        TempData["Success"] = "Notice saved.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishNotice(int id, bool publish, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        var item = await _db.Notices.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null) return NotFound();
        if (item.IsArchived) return BadRequest("Archived notices cannot be published.");

        item.IsPublished = publish;
        item.PublishedAtUtc = publish ? DateTime.UtcNow : null;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(publish ? "Notice.Publish" : "Notice.Unpublish", nameof(Notice), item.Id.ToString(), item.Title);
        TempData["Success"] = publish ? "Notice published." : "Notice unpublished.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveNotice(int id, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        var item = await _db.Notices.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null) return NotFound();
        item.IsArchived = true;
        item.IsPublished = false;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Notice.Archive", nameof(Notice), item.Id.ToString(), item.Title);
        TempData["Success"] = "Notice archived. History was preserved.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> CreateDocument(CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var model = new SchoolDocumentFormViewModel();
        await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
        return View("DocumentForm", model);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> EditDocument(int id, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        var item = await _db.SchoolDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null) return NotFound();

        var model = new SchoolDocumentFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Category = item.Category,
            Audience = item.Audience,
            SchoolClassId = item.SchoolClassId,
            SectionId = item.SectionId,
            EffectiveFromLocal = item.EffectiveFromUtc?.ToLocalTime(),
            EffectiveUntilLocal = item.EffectiveUntilUtc?.ToLocalTime(),
            IsActive = item.IsActive,
            ExistingFileName = item.OriginalFileName,
            RowVersion = Convert.ToBase64String(item.RowVersion ?? [])
        };
        await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
        return View("DocumentForm", model);
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDocument(SchoolDocumentFormViewModel model, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        var user = await _schoolContext.GetCurrentUserAsync();
        if (!schoolId.HasValue || user is null) return RedirectToAction("Index", "SchoolSetup");

        ValidateAudienceTarget(model.Audience, model.SchoolClassId, model.SectionId);
        if (model.EffectiveFromLocal.HasValue && model.EffectiveUntilLocal.HasValue && model.EffectiveUntilLocal <= model.EffectiveFromLocal)
            ModelState.AddModelError(nameof(model.EffectiveUntilLocal), "Effective-until must be later than effective-from.");
        if (!model.Id.HasValue && model.File is null)
            ModelState.AddModelError(nameof(model.File), "Select a document to upload.");

        if (!ModelState.IsValid)
        {
            await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
            return View("DocumentForm", model);
        }

        SchoolDocument entity;
        var isNew = !model.Id.HasValue;
        if (isNew)
        {
            entity = new SchoolDocument { SchoolId = schoolId.Value, UploadedByUserId = user.Id };
            _db.SchoolDocuments.Add(entity);
        }
        else
        {
            entity = await _db.SchoolDocuments.FirstOrDefaultAsync(x => x.Id == model.Id!.Value && x.SchoolId == schoolId.Value, cancellationToken)
                     ?? throw new InvalidOperationException("Document not found.");
            ApplyRowVersion(entity, model.RowVersion);
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        entity.Title = model.Title.Trim();
        entity.Description = NullIfBlank(model.Description);
        entity.Category = model.Category;
        entity.Audience = model.Audience;
        entity.SchoolClassId = model.Audience == NoticeAudience.Class ? model.SchoolClassId : null;
        entity.SectionId = model.Audience == NoticeAudience.Class ? model.SectionId : null;
        entity.EffectiveFromUtc = model.EffectiveFromLocal?.ToUniversalTime();
        entity.EffectiveUntilUtc = model.EffectiveUntilLocal?.ToUniversalTime();
        entity.IsActive = model.IsActive;

        if (model.File is not null)
        {
            var saved = await _files.SaveAsync(model.File, schoolId.Value, "document", cancellationToken);
            if (!saved.Success)
            {
                ModelState.AddModelError(nameof(model.File), saved.Message);
                await LoadAcademicOptionsAsync(model.Classes, model.Sections, schoolId.Value, cancellationToken);
                return View("DocumentForm", model);
            }
            if (!string.IsNullOrWhiteSpace(entity.StorageKey))
                await _files.DeleteAsync(entity.StorageKey, cancellationToken);

            entity.OriginalFileName = Path.GetFileName(model.File.FileName);
            entity.StorageKey = saved.StorageKey!;
            entity.ContentType = saved.ContentType ?? "application/octet-stream";
            entity.SizeBytes = saved.SizeBytes;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(isNew ? "SchoolDocument.Create" : "SchoolDocument.Update", nameof(SchoolDocument), entity.Id.ToString(), $"Title={entity.Title}; Category={entity.Category}; Audience={entity.Audience}");
        TempData["Success"] = "Document saved.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDocumentActive(int id, bool active, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var item = await _db.SchoolDocuments.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null) return NotFound();
        item.IsActive = active;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(active ? "SchoolDocument.Activate" : "SchoolDocument.Deactivate", nameof(SchoolDocument), item.Id.ToString(), item.Title);
        TempData["Success"] = active ? "Document activated." : "Document hidden. File and history were preserved.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = ManageRoles)]
    [HttpGet]
    public async Task<IActionResult> Dispatch(int id, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var item = await _db.Notices.AsNoTracking().Include(x => x.SchoolClass).Include(x => x.Section)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null) return NotFound();

        return View(new CommunicationDispatchViewModel
        {
            NoticeId = item.Id,
            NoticeTitle = item.Title,
            Audience = item.Audience,
            AudienceLabel = AudienceLabel(item),
            Channel = CommunicationChannel.InApp,
            Subject = item.Title,
            Message = item.Body.Length <= 4000 ? item.Body : item.Body[..4000]
        });
    }

    [Authorize(Roles = ManageRoles)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispatch(CommunicationDispatchViewModel model, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        var user = await _schoolContext.GetCurrentUserAsync();
        if (!schoolId.HasValue || user is null) return RedirectToAction("Index", "SchoolSetup");

        var notice = await _db.Notices.Include(x => x.SchoolClass).Include(x => x.Section)
            .FirstOrDefaultAsync(x => x.Id == model.NoticeId && x.SchoolId == schoolId.Value, cancellationToken);
        if (notice is null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.NoticeTitle = notice.Title;
            model.Audience = notice.Audience;
            model.AudienceLabel = AudienceLabel(notice);
            return View(model);
        }

        if (model.Channel == CommunicationChannel.InApp && !notice.IsPublished)
        {
            notice.IsPublished = true;
            notice.PublishedAtUtc = DateTime.UtcNow;
            notice.UpdatedAtUtc = DateTime.UtcNow;
        }

        var result = await _dispatcher.DispatchAsync(schoolId.Value, notice, model.Channel, model.Subject, model.Message.Trim(), cancellationToken);
        var history = new CommunicationHistory
        {
            SchoolId = schoolId.Value,
            NoticeId = notice.Id,
            Channel = model.Channel,
            Audience = notice.Audience,
            SchoolClassId = notice.SchoolClassId,
            SectionId = notice.SectionId,
            Subject = NullIfBlank(model.Subject),
            Message = model.Message.Trim(),
            RecipientCount = result.RecipientCount,
            Status = result.Status,
            ProviderName = result.ProviderName,
            StatusDetail = result.Detail,
            SentAtUtc = result.Status == CommunicationDeliveryStatus.Sent ? DateTime.UtcNow : null,
            CreatedByUserId = user.Id
        };
        _db.CommunicationHistory.Add(history);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Communication.Dispatch", nameof(CommunicationHistory), history.Id.ToString(), $"Channel={history.Channel}; Status={history.Status}; Recipients={history.RecipientCount}");

        TempData[result.Status == CommunicationDeliveryStatus.Sent ? "Success" : "Error"] = result.Detail;
        return RedirectToAction(nameof(Manage));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadNoticeAttachment(int id, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return NotFound();
        var item = await _db.Notices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null || string.IsNullOrWhiteSpace(item.AttachmentStorageKey)) return NotFound();
        if (!CanReadAudience(item.Audience)) return Forbid();

        var opened = await _files.OpenReadAsync(item.AttachmentStorageKey, cancellationToken);
        if (opened.Stream is null) return NotFound();
        return File(opened.Stream, item.AttachmentContentType ?? opened.ContentType, item.AttachmentOriginalName ?? "attachment");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id, CancellationToken cancellationToken)
    {
        var schoolId = await _schoolContext.GetCurrentSchoolIdAsync();
        if (!schoolId.HasValue) return NotFound();
        var item = await _db.SchoolDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId.Value, cancellationToken);
        if (item is null || !item.IsActive && !User.IsInRole(AppRoles.Principal) && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.SuperAdmin)) return NotFound();
        if (!CanReadAudience(item.Audience)) return Forbid();

        var opened = await _files.OpenReadAsync(item.StorageKey, cancellationToken);
        if (opened.Stream is null) return NotFound();
        return File(opened.Stream, item.ContentType, item.OriginalFileName);
    }

    private HashSet<NoticeAudience> GetVisibleAudiences()
    {
        var set = new HashSet<NoticeAudience> { NoticeAudience.All };
        if (User.IsInRole(AppRoles.Parent)) set.Add(NoticeAudience.Parents);
        if (User.IsInRole(AppRoles.Student)) set.Add(NoticeAudience.Students);
        if (!User.IsInRole(AppRoles.Parent) && !User.IsInRole(AppRoles.Student)) set.Add(NoticeAudience.Staff);
        return set;
    }

    private bool CanReadAudience(NoticeAudience audience)
    {
        if (User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin)
            || User.IsInRole(AppRoles.Receptionist) || User.IsInRole(AppRoles.ExamController)) return true;
        return GetVisibleAudiences().Contains(audience);
    }

    private void ValidateAudienceTarget(NoticeAudience audience, int? classId, int? sectionId)
    {
        if (audience == NoticeAudience.Class && !classId.HasValue)
            ModelState.AddModelError(nameof(classId), "Select a class for class-targeted content.");
        if (sectionId.HasValue && !classId.HasValue)
            ModelState.AddModelError(nameof(sectionId), "Select a class before selecting a section.");
    }

    private async Task LoadAcademicOptionsAsync(List<SimpleOptionViewModel> classes, List<SimpleOptionViewModel> sections, int schoolId, CancellationToken cancellationToken)
    {
        classes.Clear();
        classes.AddRange(await _db.SchoolClasses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new SimpleOptionViewModel { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken));
        sections.Clear();
        sections.AddRange(await _db.Sections.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SimpleOptionViewModel { Id = x.Id, Name = x.Name, ParentId = x.SchoolClassId })
            .ToListAsync(cancellationToken));
    }

    private void ApplyRowVersion(Notice entity, string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion)) return;
        try { _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion); }
        catch (FormatException) { }
    }

    private void ApplyRowVersion(SchoolDocument entity, string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion)) return;
        try { _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion); }
        catch (FormatException) { }
    }

    private static string AudienceLabel(Notice item)
    {
        if (item.Audience != NoticeAudience.Class) return item.Audience.ToString();
        var label = item.SchoolClass?.Name ?? "Selected class";
        if (item.Section is not null) label += $" - {item.Section.Name}";
        return label;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
