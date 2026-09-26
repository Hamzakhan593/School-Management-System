using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.UserManagers)]
public class PromotionsController : Controller
{
    private readonly IPromotionService _promotionService;
    private readonly ISchoolContextService _schoolContext;

    public PromotionsController(IPromotionService promotionService, ISchoolContextService schoolContext)
    {
        _promotionService = promotionService;
        _schoolContext = schoolContext;
    }

    public async Task<IActionResult> Index()
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        return View(await _promotionService.GetIndexAsync(schoolId.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? sourceSessionId = null)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        return View(await _promotionService.BuildCreateModelAsync(schoolId.Value, sourceSessionId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RolloverCreateViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");

        if (!ModelState.IsValid)
        {
            return View(await _promotionService.BuildCreateModelAsync(schoolId.Value, model.SourceAcademicSessionId, model));
        }

        var user = await _schoolContext.GetCurrentUserAsync();
        var result = await _promotionService.CreateRolloverAsync(schoolId.Value, user?.Id, model);
        if (!result.Success || !result.BatchId.HasValue)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _promotionService.BuildCreateModelAsync(schoolId.Value, model.SourceAcademicSessionId, model));
        }

        var preview = await _promotionService.PreparePreviewAsync(schoolId.Value, result.BatchId.Value);
        TempData[preview.Success ? "Success" : "Info"] = preview.Success ? result.Message : $"{result.Message} {preview.Message}";
        return preview.Success
            ? RedirectToAction(nameof(Preview), new { id = result.BatchId.Value })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Prepare(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var result = await _promotionService.PreparePreviewAsync(schoolId.Value, id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Preview), new { id }) : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var model = await _promotionService.GetPreviewAsync(schoolId.Value, id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreview(PromotionPreviewPostViewModel model)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var result = await _promotionService.SavePreviewAsync(schoolId.Value, model);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Preview), new { id = model.BatchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Commit(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var result = await _promotionService.CommitAsync(schoolId.Value, id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Preview), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rollback(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var result = await _promotionService.RollbackAsync(schoolId.Value, id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Preview), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int id)
    {
        var schoolId = await RequireSchoolIdAsync();
        if (!schoolId.HasValue) return RedirectToAction("Index", "SchoolSetup");
        var result = await _promotionService.FinalizeAsync(schoolId.Value, id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Preview), new { id });
    }

    private async Task<int?> RequireSchoolIdAsync() => await _schoolContext.GetCurrentSchoolIdAsync();
}
