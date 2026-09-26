using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Options;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Accountant)]
public class FeesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IFeeService _feeService;
    private readonly IFeePdfService _pdf;
    private readonly IAuditService _audit;
    private readonly FeeOptions _options;
    private readonly ChallanPrintService _challanPrint;

    public FeesController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IFeeService feeService,
        IFeePdfService pdf,
        IAuditService audit,
        IOptions<FeeOptions> options, ChallanPrintService challanPrint)
    {
        _db = db;
        _schoolContext = schoolContext;
        _feeService = feeService;
        _pdf = pdf;
        _audit = audit;
        _options = options.Value;
        _challanPrint = challanPrint;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        await _feeService.RefreshStatusesAndLateFeesAsync(context.Value.SchoolId, cancellationToken);
        var period = DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var (utcStart, utcEnd) = ToUtcRange(monthStart, monthEnd);

        var current = await _db.FeeChallans.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId
                        && x.BillingPeriod == period
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived)
            .ToListAsync(cancellationToken);

        var currentPayments = await _db.FeePayments.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId
                        && !x.IsReversed
                        && x.PaymentDateUtc >= utcStart
                        && x.PaymentDateUtc < utcEnd)
            .ToListAsync(cancellationToken);

        var outstandingRows = await _db.FeeChallans.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .Select(x => new { x.StudentId, Balance = x.CurrentChargesTotal - x.PaidAmount, x.DueDate })
            .ToListAsync(cancellationToken);

        var model = new FeesDashboardViewModel
        {
            BillingPeriod = period,
            CurrentMonthCharges = current.Sum(x => x.CurrentChargesTotal),
            CurrentMonthCollected = currentPayments.Sum(x => x.Amount),
            TotalOutstanding = outstandingRows.Sum(x => x.Balance),
            OverdueStudentCount = outstandingRows.Where(x => x.DueDate.Date < DateTime.Today).Select(x => x.StudentId).Distinct().Count(),
            ChallansThisMonth = current.Count,
            RecentPayments = await _db.FeePayments.AsNoTracking().Include(x => x.Student)
                .Where(x => x.SchoolId == context.Value.SchoolId)
                .OrderByDescending(x => x.PaymentDateUtc).Take(8).ToListAsync(cancellationToken),
            RecentChallans = await _db.FeeChallans.AsNoTracking().Include(x => x.Student)
                .Where(x => x.SchoolId == context.Value.SchoolId && !x.IsSuperseded)
                .OrderByDescending(x => x.CreatedAtUtc).Take(8).ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> FeeHeads(int? editId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var model = new FeeHeadsViewModel
        {
            Heads = await _db.FeeHeads.AsNoTracking()
                .Where(x => x.SchoolId == context.Value.SchoolId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken)
        };
        if (editId.HasValue)
        {
            var item = await _db.FeeHeads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == editId.Value && x.SchoolId == context.Value.SchoolId, cancellationToken);
            if (item is not null)
            {
                model.Form = new FeeHeadFormViewModel
                {
                    Id = item.Id, Code = item.Code, Name = item.Name, DefaultFrequency = item.DefaultFrequency,
                    DefaultAmount = item.DefaultAmount, SortOrder = item.SortOrder, IsActive = item.IsActive
                };
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> SaveFeeHead(FeeHeadFormViewModel form, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        form.Code = (form.Code ?? string.Empty).Trim().ToUpperInvariant();
        form.Name = (form.Name ?? string.Empty).Trim();

        if (await _db.FeeHeads.AnyAsync(x => x.SchoolId == context.Value.SchoolId && x.Code == form.Code && (!form.Id.HasValue || x.Id != form.Id.Value), cancellationToken))
            ModelState.AddModelError(nameof(form.Code), "This fee-head code already exists.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(FeeHeads), new { editId = form.Id });
        }

        FeeHead entity;
        if (form.Id.HasValue)
        {
            entity = await _db.FeeHeads.FirstOrDefaultAsync(x => x.Id == form.Id.Value && x.SchoolId == context.Value.SchoolId, cancellationToken)
                ?? throw new InvalidOperationException("Fee head not found.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            entity = new FeeHead { SchoolId = context.Value.SchoolId };
            _db.FeeHeads.Add(entity);
        }

        entity.Code = form.Code;
        entity.Name = form.Name;
        entity.DefaultFrequency = form.DefaultFrequency;
        entity.DefaultAmount = decimal.Round(form.DefaultAmount, 2);
        entity.SortOrder = form.SortOrder;
        entity.IsActive = form.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(form.Id.HasValue ? "FeeHead.Update" : "FeeHead.Create", nameof(FeeHead), entity.Id.ToString(), $"{entity.Code} - {entity.Name}");
        TempData["Success"] = "Fee head saved.";
        return RedirectToAction(nameof(FeeHeads));
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> Structures(int? editId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await BuildStructuresViewModelAsync(context.Value.SchoolId, cancellationToken);
        if (editId.HasValue)
        {
            var item = await _db.FeeStructures.AsNoTracking().FirstOrDefaultAsync(x => x.Id == editId.Value && x.SchoolId == context.Value.SchoolId, cancellationToken);
            if (item is not null)
            {
                model.Form = new FeeStructureFormViewModel
                {
                    Id = item.Id, AcademicSessionId = item.AcademicSessionId, FeeHeadId = item.FeeHeadId, Scope = item.Scope,
                    SchoolClassId = item.SchoolClassId, StudentId = item.StudentId, TermId = item.TermId, Frequency = item.Frequency,
                    Amount = item.Amount, EffectiveFrom = item.EffectiveFrom, EffectiveTo = item.EffectiveTo, ChargeDate = item.ChargeDate,
                    Notes = item.Notes, IsActive = item.IsActive
                };
            }
        }
        else if (model.Form.AcademicSessionId == 0)
        {
            model.Form.AcademicSessionId = model.Sessions.FirstOrDefault()?.Id ?? 0;
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> SaveStructure(FeeStructureFormViewModel form, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        if (form.Scope == FeeStructureScope.Class && !form.SchoolClassId.HasValue)
            ModelState.AddModelError(nameof(form.SchoolClassId), "Select a class for a class fee structure.");
        if (form.Scope == FeeStructureScope.Student && !form.StudentId.HasValue)
            ModelState.AddModelError(nameof(form.StudentId), "Select a student for a student-specific fee structure.");
        if (form.Frequency == FeeFrequency.TermBased && !form.TermId.HasValue)
            ModelState.AddModelError(nameof(form.TermId), "Select the term for a term-based fee.");
        if (form.Frequency is FeeFrequency.OneTime or FeeFrequency.Custom && !form.ChargeDate.HasValue)
            ModelState.AddModelError(nameof(form.ChargeDate), "Select the charge date for one-time/custom fees.");
        if (form.EffectiveFrom.HasValue && form.EffectiveTo.HasValue && form.EffectiveFrom > form.EffectiveTo)
            ModelState.AddModelError(nameof(form.EffectiveTo), "Effective To cannot be before Effective From.");

        var sessionValid = await _db.AcademicSessions.AnyAsync(x => x.Id == form.AcademicSessionId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        var headValid = await _db.FeeHeads.AnyAsync(x => x.Id == form.FeeHeadId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (!sessionValid) ModelState.AddModelError(nameof(form.AcademicSessionId), "Invalid academic session.");
        if (!headValid) ModelState.AddModelError(nameof(form.FeeHeadId), "Invalid fee head.");

        var targetId = form.Scope == FeeStructureScope.Student ? form.StudentId : form.SchoolClassId;
        if (targetId.HasValue)
        {
            var duplicate = await _db.FeeStructures.AnyAsync(x => x.SchoolId == context.Value.SchoolId
                && x.AcademicSessionId == form.AcademicSessionId
                && x.FeeHeadId == form.FeeHeadId
                && x.Scope == form.Scope
                && (form.Scope == FeeStructureScope.Student ? x.StudentId == form.StudentId : x.SchoolClassId == form.SchoolClassId)
                && (!form.Id.HasValue || x.Id != form.Id.Value), cancellationToken);
            if (duplicate) ModelState.AddModelError(string.Empty, "A fee structure for this fee head and target already exists. Edit that row instead.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Structures), new { editId = form.Id });
        }

        FeeStructure entity;
        if (form.Id.HasValue)
        {
            entity = await _db.FeeStructures.FirstOrDefaultAsync(x => x.Id == form.Id.Value && x.SchoolId == context.Value.SchoolId, cancellationToken)
                ?? throw new InvalidOperationException("Fee structure not found.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            entity = new FeeStructure { SchoolId = context.Value.SchoolId };
            _db.FeeStructures.Add(entity);
        }

        entity.AcademicSessionId = form.AcademicSessionId;
        entity.FeeHeadId = form.FeeHeadId;
        entity.Scope = form.Scope;
        entity.SchoolClassId = form.Scope == FeeStructureScope.Class ? form.SchoolClassId : null;
        entity.StudentId = form.Scope == FeeStructureScope.Student ? form.StudentId : null;
        entity.TermId = form.Frequency == FeeFrequency.TermBased ? form.TermId : null;
        entity.Frequency = form.Frequency;
        entity.Amount = decimal.Round(form.Amount, 2);
        entity.EffectiveFrom = form.EffectiveFrom?.Date;
        entity.EffectiveTo = form.EffectiveTo?.Date;
        entity.ChargeDate = form.Frequency is FeeFrequency.OneTime or FeeFrequency.Custom ? form.ChargeDate?.Date : null;
        entity.Notes = form.Notes?.Trim();
        entity.IsActive = form.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(form.Id.HasValue ? "FeeStructure.Update" : "FeeStructure.Create", nameof(FeeStructure), entity.Id.ToString(), $"FeeHeadId={entity.FeeHeadId}; Scope={entity.Scope}; Amount={entity.Amount}");
        TempData["Success"] = "Fee structure saved.";
        return RedirectToAction(nameof(Structures));
    }

    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> Discounts(int? editId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await BuildDiscountsViewModelAsync(context.Value.SchoolId, cancellationToken);
        if (editId.HasValue)
        {
            var item = await _db.StudentDiscounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == editId.Value && x.SchoolId == context.Value.SchoolId, cancellationToken);
            if (item is not null)
            {
                model.Form = new StudentDiscountFormViewModel
                {
                    Id = item.Id, StudentId = item.StudentId, FeeHeadId = item.FeeHeadId, DiscountType = item.DiscountType,
                    Value = item.Value, StartDate = item.StartDate, EndDate = item.EndDate, ApprovalNote = item.ApprovalNote, IsActive = item.IsActive
                };
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> SaveDiscount(StudentDiscountFormViewModel form, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (form.DiscountType == DiscountType.Percentage && form.Value > 100)
            ModelState.AddModelError(nameof(form.Value), "Percentage discount cannot exceed 100%.");
        if (form.EndDate.HasValue && form.EndDate.Value.Date < form.StartDate.Date)
            ModelState.AddModelError(nameof(form.EndDate), "End date cannot be before start date.");
        if (!await _db.Students.AnyAsync(x => x.Id == form.StudentId && x.SchoolId == context.Value.SchoolId, cancellationToken))
            ModelState.AddModelError(nameof(form.StudentId), "Invalid student.");
        if (form.FeeHeadId.HasValue && !await _db.FeeHeads.AnyAsync(x => x.Id == form.FeeHeadId.Value && x.SchoolId == context.Value.SchoolId, cancellationToken))
            ModelState.AddModelError(nameof(form.FeeHeadId), "Invalid fee head.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Discounts), new { editId = form.Id });
        }

        StudentDiscount entity;
        if (form.Id.HasValue)
        {
            entity = await _db.StudentDiscounts.FirstOrDefaultAsync(x => x.Id == form.Id.Value && x.SchoolId == context.Value.SchoolId, cancellationToken)
                ?? throw new InvalidOperationException("Discount not found.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            entity = new StudentDiscount { SchoolId = context.Value.SchoolId };
            _db.StudentDiscounts.Add(entity);
        }
        entity.StudentId = form.StudentId;
        entity.FeeHeadId = form.FeeHeadId;
        entity.DiscountType = form.DiscountType;
        entity.Value = decimal.Round(form.Value, 2);
        entity.StartDate = form.StartDate.Date;
        entity.EndDate = form.EndDate?.Date;
        entity.ApprovalNote = form.ApprovalNote.Trim();
        entity.IsActive = form.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(form.Id.HasValue ? "StudentDiscount.Update" : "StudentDiscount.Create", nameof(StudentDiscount), entity.Id.ToString(), $"StudentId={entity.StudentId}; Type={entity.DiscountType}; Value={entity.Value}");
        TempData["Success"] = "Student discount saved.";
        return RedirectToAction(nameof(Discounts));
    }

    public async Task<IActionResult> Generate(int? studentId, int? academicSessionId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await BuildGenerationViewModelAsync(context.Value.SchoolId, cancellationToken);
        if (academicSessionId.HasValue) model.AcademicSessionId = academicSessionId.Value;
        if (studentId.HasValue)
        {
            model.Scope = FeeBatchScope.Individual;
            model.StudentId = studentId.Value;
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(ChallanGenerationViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        ValidateGenerationRequest(model);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Generate));
        }

        model.Preview = await _feeService.PreviewGenerationAsync(context.Value.SchoolId, model, cancellationToken);
        model.PreviewToken = FeeService.CreateBatchToken();
        model.IsPreview = true;
        await PopulateGenerationLookupsAsync(context.Value.SchoolId, model, cancellationToken);
        return View("Generate", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Commit(ChallanGenerationViewModel model, CancellationToken cancellationToken, bool download = false)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        ValidateGenerationRequest(model);
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.PreviewToken))
        {
            TempData["Error"] = "Preview the batch again before committing.";
            return RedirectToAction(nameof(Generate));
        }

        try
        {
            var batch = await _feeService.GenerateAsync(context.Value.SchoolId, context.Value.UserId, model, cancellationToken);
            await _audit.WriteAsync("FeeChallanBatch.Generate", nameof(FeeChallanBatch), batch.Id.ToString(), $"Period={batch.BillingPeriod}; Scope={batch.Scope}; Generated={batch.GeneratedCount}; Skipped={batch.SkippedCount}; Total={batch.TotalAmount}");
            if (download)
            {
                var eligible = await _feeService.PreviewGenerationAsync(context.Value.SchoolId, model, cancellationToken);
                var studentIds = eligible.Rows.Select(x => x.StudentId).ToList();
                var printable = await _db.FeeChallans.AsNoTracking().Include(x => x.Student).Include(x => x.Items)
                    .Where(x => x.SchoolId == context.Value.SchoolId && x.AcademicSessionId == model.AcademicSessionId
                        && x.BillingPeriod == batch.BillingPeriod && studentIds.Contains(x.StudentId)
                        && !x.IsSuperseded && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived)
                    .OrderBy(x => x.ClassNameSnapshot).ThenBy(x => x.SectionNameSnapshot).ThenBy(x => x.Student.FullName).ToListAsync(cancellationToken);
                return await PrintAsync(context.Value.SchoolId, printable, $"Challans-{batch.BillingPeriod}.pdf", cancellationToken);
            }
            TempData["Success"] = $"Batch completed: {batch.GeneratedCount} challan(s) generated, {batch.SkippedCount} skipped.";
            return RedirectToAction(nameof(Challans), new { billingPeriod = batch.BillingPeriod, batchId = batch.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Generate));
        }
    }

    public async Task<IActionResult> Challans(int? batchId, string? billingPeriod, FeeChallanStatus? status, int? schoolClassId, string? search, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        await _feeService.RefreshStatusesAndLateFeesAsync(context.Value.SchoolId, cancellationToken);

        var query = _db.FeeChallans.AsNoTracking().Include(x => x.Student)
            .Where(x => x.SchoolId == context.Value.SchoolId && !x.IsSuperseded);
        if (batchId.HasValue) query = query.Where(x => x.FeeChallanBatchId == batchId.Value);
        if (!string.IsNullOrWhiteSpace(billingPeriod)) query = query.Where(x => x.BillingPeriod == billingPeriod);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (schoolClassId.HasValue)
        {
            var classStudentIds = _db.StudentEnrollments.Where(x => x.SchoolId == context.Value.SchoolId && x.IsCurrent && x.SchoolClassId == schoolClassId.Value).Select(x => x.StudentId);
            query = query.Where(x => classStudentIds.Contains(x.StudentId));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(x => x.ChallanNumber.Contains(q) || x.Student.FullName.Contains(q) || x.Student.AdmissionNumber.Contains(q));
        }

        var model = new ChallansViewModel
        {
            BatchId = batchId,
            BillingPeriod = billingPeriod,
            Status = status,
            SchoolClassId = schoolClassId,
            Search = search,
            Challans = await query.OrderByDescending(x => x.BillingPeriod).ThenBy(x => x.Student.FullName).Take(1500).ToListAsync(cancellationToken),
            Classes = await GetClassOptionsAsync(context.Value.SchoolId, cancellationToken)
        };
        return View(model);
    }

    public async Task<IActionResult> ChallanDetails(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        await _feeService.RefreshStatusesAndLateFeesAsync(context.Value.SchoolId, cancellationToken);
        var challan = await _db.FeeChallans.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Items)
            .Include(x => x.PaymentAllocations).ThenInclude(x => x.FeePayment)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (challan is null) return NotFound();
        var activeAllocated = challan.PaymentAllocations.Where(x => !x.FeePayment.IsReversed).Sum(x => x.Amount);
        return View(new FeeChallanDetailsViewModel
        {
            Challan = challan,
            CurrentStudentOutstanding = await _feeService.GetStudentOutstandingAsync(context.Value.SchoolId, challan.StudentId, cancellationToken),
            ActiveAllocatedAmount = activeAllocated
        });
    }

    public async Task<IActionResult> ChallanPdf(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var school = await _db.Schools.AsNoTracking().FirstAsync(x => x.Id == context.Value.SchoolId, cancellationToken);
        var challan = await _db.FeeChallans.AsNoTracking().Include(x => x.Student).Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (challan is null) return NotFound();
        if (challan.IsSuperseded || challan.Status is FeeChallanStatus.Cancelled or FeeChallanStatus.Waived)
        {
            TempData["Error"] = "Cancelled, waived or replaced challans cannot be printed for payment.";
            return RedirectToAction(nameof(ChallanDetails), new { id });
        }
        return await PrintAsync(context.Value.SchoolId, new[] { challan }, $"{challan.ChallanNumber}.pdf", cancellationToken);
    }

    public async Task<IActionResult> BatchPdf(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var batch = await _db.FeeChallanBatches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (batch is null) return NotFound();
        var challans = await _db.FeeChallans.AsNoTracking().Include(x => x.Student).Include(x => x.Items)
            .Where(x => x.FeeChallanBatchId == id && x.SchoolId == context.Value.SchoolId && !x.IsSuperseded
                && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived)
            .OrderBy(x => x.ClassNameSnapshot).ThenBy(x => x.SectionNameSnapshot).ThenBy(x => x.Student.FullName).ToListAsync(cancellationToken);
        return await PrintAsync(context.Value.SchoolId, challans, $"Challans-{batch.BillingPeriod}-Batch-{batch.Id}.pdf", cancellationToken);
    }

    public async Task<IActionResult> ExportPdf(string? billingPeriod, int? schoolClassId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        billingPeriod = string.IsNullOrWhiteSpace(billingPeriod) ? DateTime.Today.ToString("yyyy-MM") : billingPeriod;
        if (!DateTime.TryParseExact(billingPeriod, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return BadRequest("Select a valid billing month.");
        var query = _db.FeeChallans.AsNoTracking().Include(x => x.Student).Include(x => x.Items)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.BillingPeriod == billingPeriod && !x.IsSuperseded
                && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived);
        if (schoolClassId.HasValue)
        {
            var students = _db.StudentEnrollments.Where(x => x.SchoolId == context.Value.SchoolId && x.IsCurrent && x.SchoolClassId == schoolClassId).Select(x => x.StudentId);
            query = query.Where(x => students.Contains(x.StudentId));
        }
        var rows = await query.OrderBy(x => x.ClassNameSnapshot).ThenBy(x => x.SectionNameSnapshot).ThenBy(x => x.Student.FullName).ToListAsync(cancellationToken);
        return await PrintAsync(context.Value.SchoolId, rows, $"Challans-{billingPeriod}.pdf", cancellationToken);
    }

    private async Task<IActionResult> PrintAsync(int schoolId, IReadOnlyList<FeeChallan> challans, string filename, CancellationToken ct)
    {
        if (challans.Count == 0)
        {
            TempData["Error"] = "No printable challans found. Generate the selected month's fees first.";
            return RedirectToAction(nameof(Generate));
        }
        // Reuse the existing policy, then reload so newly applied fees and partial payments appear in the PDF.
        await _feeService.RefreshStatusesAndLateFeesAsync(schoolId, ct);
        var ids = challans.Select(x => x.Id).ToList();
        var fresh = await _db.FeeChallans.AsNoTracking().Include(x => x.Student).Include(x => x.Items)
            .Where(x => x.SchoolId == schoolId && ids.Contains(x.Id) && !x.IsSuperseded
                && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived)
            .OrderBy(x => x.ClassNameSnapshot).ThenBy(x => x.SectionNameSnapshot).ThenBy(x => x.Student.FullName).ToListAsync(ct);
        if (fresh.Count == 0) return RedirectToAction(nameof(Generate));
        var school = await _db.Schools.AsNoTracking().FirstAsync(x => x.Id == schoolId, ct);
        var models = await _challanPrint.BuildAsync(schoolId, fresh, ct);
        return File(_pdf.CreateChallanBatchPdf(school, models), "application/pdf", filename);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> Regenerate(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        try
        {
            var newChallan = await _feeService.RegenerateChallanAsync(context.Value.SchoolId, id, context.Value.UserId, cancellationToken);
            await _audit.WriteAsync("FeeChallan.Regenerate", nameof(FeeChallan), newChallan.Id.ToString(), $"Replacement={newChallan.ChallanNumber}; Version={newChallan.Version}");
            TempData["Success"] = $"Challan regenerated as {newChallan.ChallanNumber}.";
            return RedirectToAction(nameof(ChallanDetails), new { id = newChallan.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(ChallanDetails), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> CancelChallan(int id, string reason, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        try
        {
            await _feeService.CancelChallanAsync(context.Value.SchoolId, id, reason, context.Value.UserId, cancellationToken);
            await _audit.WriteAsync("FeeChallan.Cancel", nameof(FeeChallan), id.ToString(), reason);
            TempData["Success"] = "Challan cancelled. Financial history was retained.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ChallanDetails), new { id });
    }

    public async Task<IActionResult> CollectPayment(int studentId, int? fromChallanId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        await _feeService.RefreshStatusesAndLateFeesAsync(context.Value.SchoolId, cancellationToken);
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == studentId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (student is null) return NotFound();
        var outstanding = await _feeService.GetStudentOutstandingAsync(context.Value.SchoolId, studentId, cancellationToken);
        decimal suggested = outstanding;
        if (fromChallanId.HasValue)
        {
            var challan = await _db.FeeChallans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == fromChallanId.Value && x.StudentId == studentId && x.SchoolId == context.Value.SchoolId, cancellationToken);
            if (challan is not null) suggested = Math.Min(outstanding, Math.Max(0m, challan.CurrentChargesTotal - challan.PaidAmount));
        }
        return View(new PaymentEntryViewModel
        {
            StudentId = studentId,
            FromChallanId = fromChallanId,
            StudentName = student.FullName,
            AdmissionNumber = student.AdmissionNumber,
            Outstanding = outstanding,
            Amount = suggested
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectPayment(PaymentEntryViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.StudentId && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (student is null) return NotFound();
        model.StudentName = student.FullName;
        model.AdmissionNumber = student.AdmissionNumber;
        model.Outstanding = await _feeService.GetStudentOutstandingAsync(context.Value.SchoolId, model.StudentId, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        try
        {
            var payment = await _feeService.ReceivePaymentAsync(context.Value.SchoolId, context.Value.UserId, model, cancellationToken);
            await _audit.WriteAsync("FeePayment.Post", nameof(FeePayment), payment.Id.ToString(), $"Receipt={payment.ReceiptNumber}; StudentId={payment.StudentId}; Amount={payment.Amount}; Method={payment.PaymentMethod}");
            return RedirectToAction(nameof(PaymentSaved), new { id = payment.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex is InvalidOperationException ? ex.Message : "Payment could not be confirmed. Retry this form; the same payment will not be recorded twice.");
            return View(model);
        }
    }

    public async Task<IActionResult> PaymentSaved(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var payment = await _db.FeePayments.AsNoTracking().Include(x => x.Student).FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (payment is null) return NotFound();
        ViewBag.Outstanding = await _feeService.GetStudentOutstandingAsync(context.Value.SchoolId, payment.StudentId, cancellationToken);
        return View(payment);
    }

    [HttpGet]
    public async Task<IActionResult> StudentSearch(string? q, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return Json(Array.Empty<object>());
        var term = q?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 2) return Json(Array.Empty<object>());
        var students = await _db.Students.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId &&
                (x.FullName.Contains(term) || x.AdmissionNumber.Contains(term) ||
                 (x.RollNumber != null && x.RollNumber.Contains(term)) ||
                 x.Enrollments.Any(e => e.IsCurrent && e.RollNumber != null && e.RollNumber.Contains(term)) ||
                 x.StudentGuardians.Any(g => g.Guardian.Phone.Contains(term))))
            .OrderBy(x => x.FullName).Take(20)
            .Select(x => new { x.Id, Name = x.FullName, x.AdmissionNumber,
                Placement = x.Enrollments.Where(e => e.IsCurrent).Select(e => e.ClassName + " / " + (e.SectionName ?? "—")).FirstOrDefault() ?? "Class not assigned" })
            .ToListAsync(cancellationToken);
        return Json(students);
    }

    public async Task<IActionResult> ReceiptPdf(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var school = await _db.Schools.AsNoTracking().FirstAsync(x => x.Id == context.Value.SchoolId, cancellationToken);
        var payment = await _db.FeePayments.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Allocations).ThenInclude(x => x.FeeChallan)
            .FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (payment is null) return NotFound();
        var outstanding = await _feeService.GetStudentOutstandingAsync(context.Value.SchoolId, payment.StudentId, cancellationToken);
        return File(_pdf.CreateReceiptPdf(school, payment, outstanding), "application/pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin)]
    public async Task<IActionResult> ReversePayment(int id, string reason, int studentId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        try
        {
            await _feeService.ReversePaymentAsync(context.Value.SchoolId, id, reason, context.Value.UserId, cancellationToken);
            await _audit.WriteAsync("FeePayment.Reverse", nameof(FeePayment), id.ToString(), reason);
            TempData["Success"] = "Payment reversed. The original receipt remains in history.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Ledger), new { studentId });
    }

    public async Task<IActionResult> Ledger(int? studentId, string? search, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        await _feeService.RefreshStatusesAndLateFeesAsync(context.Value.SchoolId, cancellationToken);

        var model = new StudentLedgerViewModel
        {
            StudentOptions = await GetStudentOptionsAsync(context.Value.SchoolId, cancellationToken)
        };
        if (!studentId.HasValue) return View(model);

        model.Student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == studentId.Value && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (model.Student is null) return NotFound();
        model.Challans = await _db.FeeChallans.AsNoTracking().Include(x => x.Items)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.StudentId == studentId.Value && !x.IsSuperseded)
            .OrderByDescending(x => x.BillingPeriod).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);
        model.Payments = await _db.FeePayments.AsNoTracking().Include(x => x.Allocations).ThenInclude(x => x.FeeChallan)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.StudentId == studentId.Value)
            .OrderByDescending(x => x.PaymentDateUtc).ToListAsync(cancellationToken);
        model.TotalCharges = model.Challans.Where(x => x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived).Sum(x => x.CurrentChargesTotal);
        model.TotalPaid = model.Payments.Where(x => !x.IsReversed).Sum(x => x.Amount);
        model.TotalOutstanding = await _feeService.GetStudentOutstandingAsync(context.Value.SchoolId, studentId.Value, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Defaulters(DateTime? asOfDate, int? schoolClassId, decimal? minimumAmount, int? minimumOverduePeriods, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        await _feeService.RefreshStatusesAndLateFeesAsync(context.Value.SchoolId, cancellationToken);
        var asOf = (asOfDate ?? DateTime.Today).Date;

        var query = _db.FeeChallans.AsNoTracking().Include(x => x.Student)
            .Where(x => x.SchoolId == context.Value.SchoolId
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.DueDate < asOf
                        && x.CurrentChargesTotal > x.PaidAmount);
        if (schoolClassId.HasValue)
        {
            var ids = _db.StudentEnrollments.Where(x => x.SchoolId == context.Value.SchoolId && x.IsCurrent && x.SchoolClassId == schoolClassId.Value).Select(x => x.StudentId);
            query = query.Where(x => ids.Contains(x.StudentId));
        }
        var overdue = await query.ToListAsync(cancellationToken);
        var currentEnrollments = await _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId && x.IsCurrent)
            .ToDictionaryAsync(x => x.StudentId, cancellationToken);
        var rows = overdue.GroupBy(x => new { x.StudentId, x.Student.FullName, x.Student.AdmissionNumber })
            .Select(g =>
            {
                currentEnrollments.TryGetValue(g.Key.StudentId, out var enrollment);
                return new DefaulterRowViewModel
                {
                    StudentId = g.Key.StudentId,
                    StudentName = g.Key.FullName,
                    AdmissionNumber = g.Key.AdmissionNumber,
                    ClassSection = enrollment is null ? "-" : (string.IsNullOrWhiteSpace(enrollment.SectionName) ? enrollment.ClassName : $"{enrollment.ClassName} / {enrollment.SectionName}"),
                    OverduePeriods = g.Count(),
                    OldestDueDate = g.Min(x => x.DueDate),
                    Outstanding = g.Sum(x => x.CurrentChargesTotal - x.PaidAmount)
                };
            })
            .Where(x => !minimumAmount.HasValue || x.Outstanding >= minimumAmount.Value)
            .Where(x => !minimumOverduePeriods.HasValue || x.OverduePeriods >= minimumOverduePeriods.Value)
            .OrderByDescending(x => x.Outstanding).ThenBy(x => x.StudentName).ToList();

        return View(new DefaulterReportViewModel
        {
            AsOfDate = asOf,
            SchoolClassId = schoolClassId,
            MinimumAmount = minimumAmount,
            MinimumOverduePeriods = minimumOverduePeriods,
            Classes = await GetClassOptionsAsync(context.Value.SchoolId, cancellationToken),
            Rows = rows
        });
    }

    public async Task<IActionResult> DailyCollection(DateTime? date, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var target = (date ?? DateTime.Today).Date;
        var (utcStart, utcEnd) = ToUtcRange(target, target.AddDays(1));
        var payments = await _db.FeePayments.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Allocations).ThenInclude(x => x.FeeChallan)
            .Where(x => x.SchoolId == context.Value.SchoolId && x.PaymentDateUtc >= utcStart && x.PaymentDateUtc < utcEnd)
            .OrderBy(x => x.PaymentDateUtc).ToListAsync(cancellationToken);

        var receiverIds = payments.Where(x => !string.IsNullOrWhiteSpace(x.ReceivedByUserId)).Select(x => x.ReceivedByUserId!).Distinct().ToList();
        var receiverNames = await _db.Users.AsNoTracking().Where(x => receiverIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        var active = payments.Where(x => !x.IsReversed).ToList();
        var byReceiver = active.GroupBy(x => string.IsNullOrWhiteSpace(x.ReceivedByUserId) ? "System" : receiverNames.GetValueOrDefault(x.ReceivedByUserId!, x.ReceivedByUserId!))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var classRows = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var payment in active)
        {
            foreach (var allocation in payment.Allocations)
            {
                var key = string.IsNullOrWhiteSpace(allocation.FeeChallan.ClassNameSnapshot) ? "Unassigned" : allocation.FeeChallan.ClassNameSnapshot!;
                classRows[key] = classRows.GetValueOrDefault(key) + allocation.Amount;
            }
        }
        return View(new DailyCollectionViewModel
        {
            Date = target,
            Payments = payments,
            Total = active.Sum(x => x.Amount),
            TotalsByMethod = active.GroupBy(x => x.PaymentMethod).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount)),
            TotalsByReceiver = byReceiver,
            TotalsByClass = classRows.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value)
        });
    }

    public async Task<IActionResult> ExportDefaultersCsv(DateTime? asOfDate, CancellationToken cancellationToken)
    {
        var result = await Defaulters(asOfDate, null, null, null, cancellationToken) as ViewResult;
        if (result?.Model is not DefaulterReportViewModel model) return BadRequest();
        var sb = new StringBuilder("Admission No,Student,Class/Section,Overdue Periods,Oldest Due,Outstanding\r\n");
        foreach (var row in model.Rows)
            sb.AppendLine($"{Csv(row.AdmissionNumber)},{Csv(row.StudentName)},{Csv(row.ClassSection)},{row.OverduePeriods},{row.OldestDueDate:yyyy-MM-dd},{row.Outstanding.ToString("0.00", CultureInfo.InvariantCulture)}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"Defaulters-{model.AsOfDate:yyyy-MM-dd}.csv");
    }

    private async Task<FeeStructuresViewModel> BuildStructuresViewModelAsync(int schoolId, CancellationToken ct)
    {
        var model = new FeeStructuresViewModel
        {
            Structures = await _db.FeeStructures.AsNoTracking().Include(x => x.AcademicSession).Include(x => x.FeeHead).Include(x => x.SchoolClass).Include(x => x.Student).Include(x => x.Term)
                .Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.AcademicSession.StartDate).ThenBy(x => x.FeeHead.Name).ToListAsync(ct),
            Sessions = await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.StartDate).Select(x => new LookupOption { Id = x.Id, Text = x.Name }).ToListAsync(ct),
            FeeHeads = await _db.FeeHeads.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => new LookupOption { Id = x.Id, Text = x.Name }).ToListAsync(ct),
            Classes = await GetClassOptionsAsync(schoolId, ct),
            Students = await GetStudentOptionsAsync(schoolId, ct),
            Terms = await _db.Terms.AsNoTracking().Where(x => x.AcademicSession.SchoolId == schoolId).OrderBy(x => x.AcademicSession.StartDate).ThenBy(x => x.DisplayOrder).Select(x => new LookupOption { Id = x.Id, Text = x.AcademicSession.Name + " - " + x.Name }).ToListAsync(ct)
        };
        model.Form.AcademicSessionId = model.Sessions.FirstOrDefault()?.Id ?? 0;
        return model;
    }

    private async Task<StudentDiscountsViewModel> BuildDiscountsViewModelAsync(int schoolId, CancellationToken ct)
        => new()
        {
            Discounts = await _db.StudentDiscounts.AsNoTracking().Include(x => x.Student).Include(x => x.FeeHead)
                .Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.IsActive).ThenBy(x => x.Student.FullName).ToListAsync(ct),
            Students = await GetStudentOptionsAsync(schoolId, ct),
            FeeHeads = await _db.FeeHeads.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).OrderBy(x => x.Name).Select(x => new LookupOption { Id = x.Id, Text = x.Name }).ToListAsync(ct)
        };

    private async Task<ChallanGenerationViewModel> BuildGenerationViewModelAsync(int schoolId, CancellationToken ct)
    {
        var activeSession = await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        var model = new ChallanGenerationViewModel
        {
            AcademicSessionId = activeSession ?? 0,
            BillingMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            Scope = FeeBatchScope.WholeSchool
        };
        await PopulateGenerationLookupsAsync(schoolId, model, ct);
        return model;
    }

    private async Task PopulateGenerationLookupsAsync(int schoolId, ChallanGenerationViewModel model, CancellationToken ct)
    {
        model.Sessions = await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.StartDate).Select(x => new LookupOption { Id = x.Id, Text = x.Name }).ToListAsync(ct);
        model.Classes = await GetClassOptionsAsync(schoolId, ct);
        model.Sections = await _db.Sections.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).OrderBy(x => x.SchoolClass.SortOrder).ThenBy(x => x.Name).Select(x => new LookupOption { Id = x.Id, Text = x.SchoolClass.Name + " / " + x.Name }).ToListAsync(ct);
        model.Students = await GetStudentOptionsAsync(schoolId, ct);
    }

    private void ValidateGenerationRequest(ChallanGenerationViewModel model)
    {
        if (model.AcademicSessionId <= 0) ModelState.AddModelError(nameof(model.AcademicSessionId), "Select an academic session.");
        if (model.Scope == FeeBatchScope.Individual && !model.StudentId.HasValue) ModelState.AddModelError(nameof(model.StudentId), "Select a student.");
        if (model.Scope == FeeBatchScope.ClassSection && !model.SchoolClassId.HasValue) ModelState.AddModelError(nameof(model.SchoolClassId), "Select a class.");
        if (model.Scope == FeeBatchScope.SelectedStudents && model.SelectedStudentIds.Count == 0) ModelState.AddModelError(nameof(model.SelectedStudentIds), "Select at least one student.");
    }

    private async Task<IReadOnlyList<LookupOption>> GetClassOptionsAsync(int schoolId, CancellationToken ct)
        => await _db.SchoolClasses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new LookupOption { Id = x.Id, Text = x.Name }).ToListAsync(ct);

    private async Task<IReadOnlyList<LookupOption>> GetStudentOptionsAsync(int schoolId, CancellationToken ct)
        => await _db.Students.AsNoTracking().Where(x => x.SchoolId == schoolId && x.Status == StudentStatus.Active).OrderBy(x => x.FullName)
            .Select(x => new LookupOption { Id = x.Id, Text = x.FullName + " · " + x.AdmissionNumber + " · " + (x.Enrollments.Where(e => e.IsCurrent).Select(e => e.ClassName + " / " + e.SectionName).FirstOrDefault() ?? "Unassigned") }).ToListAsync(ct);

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is not int schoolId)
            return null;
        return (schoolId, user.Id);
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school before using Fees.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private (DateTime UtcStart, DateTime UtcEnd) ToUtcRange(DateTime localStart, DateTime localEnd)
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(_options.SchoolTimeZoneId);
            return (TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified), zone), TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified), zone));
        }
        catch
        {
            return (localStart.AddHours(-5), localEnd.AddHours(-5));
        }
    }

    private static string Csv(string value) => '"' + value.Replace("\"", "\"\"") + '"';
}
