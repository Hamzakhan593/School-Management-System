using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Accountant)]
public class ExpensesController : Controller
{
    // M19 can later move this value to school-specific master settings.
    private const decimal HighValueApprovalThreshold = 50_000m;
    private const string ApprovalRoles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin;

    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IExpenseFileService _fileService;
    private readonly IFinanceService _finance;
    private readonly IAuditService _audit;

    public ExpensesController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IExpenseFileService fileService,
        IFinanceService finance,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _fileService = fileService;
        _finance = finance;
        _audit = audit;
    }

    public async Task<IActionResult> Index(int? year, int? month, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var (y, m) = NormalizePeriod(year, month);
        return View(await _finance.BuildExpensesIndexAsync(context.Value.SchoolId, y, m, cancellationToken));
    }

    [Authorize(Roles = ApprovalRoles)]
    public async Task<IActionResult> Categories(int? editId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var model = new ExpenseCategoriesViewModel
        {
            Categories = await _db.ExpenseCategories.AsNoTracking()
                .Where(x => x.SchoolId == context.Value.SchoolId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .ToListAsync(cancellationToken)
        };

        if (editId.HasValue)
        {
            var item = await _db.ExpenseCategories.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == editId.Value && x.SchoolId == context.Value.SchoolId, cancellationToken);
            if (item is not null)
            {
                model.Form = new ExpenseCategoryFormViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    SortOrder = item.SortOrder,
                    IsActive = item.IsActive
                };
            }
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<IActionResult> SaveCategory(ExpenseCategoryFormViewModel form, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        form.Name = (form.Name ?? string.Empty).Trim();
        if (await _db.ExpenseCategories.AnyAsync(x => x.SchoolId == context.Value.SchoolId
                                                       && x.Name == form.Name
                                                       && (!form.Id.HasValue || x.Id != form.Id.Value), cancellationToken))
            ModelState.AddModelError(nameof(form.Name), "This expense category already exists.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Categories), new { editId = form.Id });
        }

        ExpenseCategory entity;
        if (form.Id.HasValue)
        {
            entity = await _db.ExpenseCategories.FirstOrDefaultAsync(x => x.Id == form.Id.Value && x.SchoolId == context.Value.SchoolId, cancellationToken)
                     ?? throw new InvalidOperationException("Expense category not found.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            entity = new ExpenseCategory { SchoolId = context.Value.SchoolId };
            _db.ExpenseCategories.Add(entity);
        }

        entity.Name = form.Name;
        entity.Description = NullIfBlank(form.Description);
        entity.SortOrder = form.SortOrder;
        entity.IsActive = form.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(form.Id.HasValue ? "ExpenseCategory.Update" : "ExpenseCategory.Create", nameof(ExpenseCategory), entity.Id.ToString(), entity.Name);

        TempData["Success"] = "Expense category saved.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var model = new ExpenseFormViewModel { ExpenseDate = DateTime.Today };
        await LoadCategoriesAsync(model, context.Value.SchoolId, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseFormViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        await ValidateExpenseFormAsync(model, context.Value.SchoolId, cancellationToken);
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model, context.Value.SchoolId, cancellationToken);
            return View(model);
        }

        var expense = new Expense
        {
            SchoolId = context.Value.SchoolId,
            ExpenseCategoryId = model.ExpenseCategoryId,
            ExpenseDate = model.ExpenseDate.Date,
            Amount = decimal.Round(model.Amount, 2),
            PaidTo = model.PaidTo.Trim(),
            PaymentMethod = model.PaymentMethod,
            ReferenceNumber = NullIfBlank(model.ReferenceNumber),
            Notes = NullIfBlank(model.Notes),
            CreatedByUserId = context.Value.UserId
        };
        ApplyApprovalRule(expense);

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(cancellationToken);

        if (model.Attachment is not null)
        {
            var saved = await _fileService.SaveAsync(model.Attachment, expense.SchoolId, expense.Id, cancellationToken);
            if (!saved.Success)
            {
                _db.Expenses.Remove(expense);
                await _db.SaveChangesAsync(cancellationToken);
                ModelState.AddModelError(nameof(model.Attachment), saved.Message);
                await LoadCategoriesAsync(model, context.Value.SchoolId, cancellationToken);
                return View(model);
            }

            expense.AttachmentOriginalName = Path.GetFileName(model.Attachment.FileName);
            expense.AttachmentStorageKey = saved.StorageKey;
            expense.AttachmentContentType = saved.ContentType;
            expense.AttachmentSizeBytes = saved.SizeBytes;
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _audit.WriteAsync("Expense.Create", nameof(Expense), expense.Id.ToString(), $"Amount={expense.Amount:0.00}; PaidTo={expense.PaidTo}; Approval={expense.ApprovalStatus}");
        TempData["Success"] = expense.ApprovalStatus == ExpenseApprovalStatus.Pending
            ? $"Expense recorded and sent for approval because it is Rs. {HighValueApprovalThreshold:N0} or above."
            : "Expense recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var expense = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (expense is null) return NotFound();
        if (expense.IsCancelled)
        {
            TempData["Error"] = "Cancelled expenses cannot be edited.";
            return RedirectToAction(nameof(Index));
        }

        var model = new ExpenseFormViewModel
        {
            Id = expense.Id,
            ExpenseCategoryId = expense.ExpenseCategoryId,
            ExpenseDate = expense.ExpenseDate,
            Amount = expense.Amount,
            PaidTo = expense.PaidTo,
            PaymentMethod = expense.PaymentMethod,
            ReferenceNumber = expense.ReferenceNumber,
            Notes = expense.Notes,
            ExistingAttachmentName = expense.AttachmentOriginalName,
            RowVersion = Convert.ToBase64String(expense.RowVersion ?? [])
        };
        await LoadCategoriesAsync(model, context.Value.SchoolId, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExpenseFormViewModel model, CancellationToken cancellationToken)
    {
        if (!model.Id.HasValue) return BadRequest();
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();

        var expense = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == model.Id.Value && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (expense is null) return NotFound();
        if (expense.IsCancelled)
        {
            TempData["Error"] = "Cancelled expenses cannot be edited.";
            return RedirectToAction(nameof(Index));
        }

        await ValidateExpenseFormAsync(model, context.Value.SchoolId, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.ExistingAttachmentName = expense.AttachmentOriginalName;
            await LoadCategoriesAsync(model, context.Value.SchoolId, cancellationToken);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.RowVersion))
        {
            try
            {
                _db.Entry(expense).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(model.RowVersion);
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "The expense version is invalid. Reload and try again.");
                model.ExistingAttachmentName = expense.AttachmentOriginalName;
                await LoadCategoriesAsync(model, context.Value.SchoolId, cancellationToken);
                return View(model);
            }
        }

        expense.ExpenseCategoryId = model.ExpenseCategoryId;
        expense.ExpenseDate = model.ExpenseDate.Date;
        expense.Amount = decimal.Round(model.Amount, 2);
        expense.PaidTo = model.PaidTo.Trim();
        expense.PaymentMethod = model.PaymentMethod;
        expense.ReferenceNumber = NullIfBlank(model.ReferenceNumber);
        expense.Notes = NullIfBlank(model.Notes);
        expense.UpdatedAtUtc = DateTime.UtcNow;
        expense.ApprovedByUserId = null;
        expense.ApprovedAtUtc = null;
        expense.RejectionReason = null;
        ApplyApprovalRule(expense);

        string? oldStorageKey = null;
        if (model.Attachment is not null)
        {
            var saved = await _fileService.SaveAsync(model.Attachment, expense.SchoolId, expense.Id, cancellationToken);
            if (!saved.Success)
            {
                ModelState.AddModelError(nameof(model.Attachment), saved.Message);
                model.ExistingAttachmentName = expense.AttachmentOriginalName;
                await LoadCategoriesAsync(model, context.Value.SchoolId, cancellationToken);
                return View(model);
            }

            oldStorageKey = expense.AttachmentStorageKey;
            expense.AttachmentOriginalName = Path.GetFileName(model.Attachment.FileName);
            expense.AttachmentStorageKey = saved.StorageKey;
            expense.AttachmentContentType = saved.ContentType;
            expense.AttachmentSizeBytes = saved.SizeBytes;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "This expense was changed by another user. Reload it and try again.";
            return RedirectToAction(nameof(Edit), new { id = expense.Id });
        }

        if (!string.IsNullOrWhiteSpace(oldStorageKey))
            await _fileService.DeleteAsync(oldStorageKey, cancellationToken);

        await _audit.WriteAsync("Expense.Update", nameof(Expense), expense.Id.ToString(), $"Amount={expense.Amount:0.00}; Approval={expense.ApprovalStatus}");
        TempData["Success"] = expense.ApprovalStatus == ExpenseApprovalStatus.Pending
            ? "Expense updated and sent for approval."
            : "Expense updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var expense = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (expense is null) return NotFound();
        if (expense.IsCancelled || expense.ApprovalStatus != ExpenseApprovalStatus.Pending)
        {
            TempData["Error"] = "This expense is not awaiting approval.";
            return RedirectToAction(nameof(Index));
        }

        expense.ApprovalStatus = ExpenseApprovalStatus.Approved;
        expense.ApprovedByUserId = context.Value.UserId;
        expense.ApprovedAtUtc = DateTime.UtcNow;
        expense.RejectionReason = null;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Expense.Approve", nameof(Expense), expense.Id.ToString(), $"Amount={expense.Amount:0.00}");
        TempData["Success"] = "Expense approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<IActionResult> Reject(int id, string? reason, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A rejection reason is required.";
            return RedirectToAction(nameof(Index));
        }

        var expense = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (expense is null) return NotFound();
        if (expense.IsCancelled || expense.ApprovalStatus != ExpenseApprovalStatus.Pending)
        {
            TempData["Error"] = "This expense is not awaiting approval.";
            return RedirectToAction(nameof(Index));
        }

        expense.ApprovalStatus = ExpenseApprovalStatus.Rejected;
        expense.ApprovedByUserId = null;
        expense.ApprovedAtUtc = null;
        expense.RejectionReason = reason.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Expense.Reject", nameof(Expense), expense.Id.ToString(), expense.RejectionReason);
        TempData["Success"] = "Expense rejected. It is excluded from financial totals.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A cancellation reason is required.";
            return RedirectToAction(nameof(Index));
        }

        var expense = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (expense is null) return NotFound();
        if (expense.IsCancelled)
        {
            TempData["Info"] = "Expense is already cancelled.";
            return RedirectToAction(nameof(Index));
        }

        expense.IsCancelled = true;
        expense.CancelledByUserId = context.Value.UserId;
        expense.CancelledAtUtc = DateTime.UtcNow;
        expense.CancellationReason = reason.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Expense.Cancel", nameof(Expense), expense.Id.ToString(), expense.CancellationReason);
        TempData["Success"] = "Expense cancelled. The historical record was preserved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Attachment(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return Unauthorized();
        var expense = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (expense is null || string.IsNullOrWhiteSpace(expense.AttachmentStorageKey)) return NotFound();

        var opened = await _fileService.OpenReadAsync(expense.AttachmentStorageKey, cancellationToken);
        return opened.Stream is null ? NotFound() : File(opened.Stream, opened.ContentType, expense.AttachmentOriginalName ?? $"expense-{id}");
    }

    public async Task<IActionResult> Report(int? year, int? month, int? categoryId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var (y, m) = NormalizePeriod(year, month);
        return View(await _finance.BuildExpenseReportAsync(context.Value.SchoolId, y, m, categoryId, cancellationToken));
    }

    public async Task<IActionResult> OtherIncome(int? year, int? month, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var (y, m) = NormalizePeriod(year, month);
        var start = new DateTime(y, m, 1);
        var end = start.AddMonths(1);
        var items = await _db.OtherIncomes.AsNoTracking()
            .Where(x => x.SchoolId == context.Value.SchoolId && x.IncomeDate >= start && x.IncomeDate < end)
            .OrderByDescending(x => x.IncomeDate).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return View(new OtherIncomeIndexViewModel
        {
            Year = y,
            Month = m,
            MonthTotal = items.Where(x => !x.IsCancelled).Sum(x => x.Amount),
            Items = items,
            Form = new OtherIncomeFormViewModel { IncomeDate = DateTime.Today }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOtherIncome(OtherIncomeFormViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (model.IncomeDate.Year < 2000 || model.IncomeDate.Date > DateTime.Today.AddDays(1))
            ModelState.AddModelError(nameof(model.IncomeDate), "Choose a valid income date.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(OtherIncome));
        }

        var income = new OtherIncome
        {
            SchoolId = context.Value.SchoolId,
            IncomeDate = model.IncomeDate.Date,
            Amount = decimal.Round(model.Amount, 2),
            Source = model.Source.Trim(),
            PaymentMethod = model.PaymentMethod,
            ReferenceNumber = NullIfBlank(model.ReferenceNumber),
            Notes = NullIfBlank(model.Notes),
            CreatedByUserId = context.Value.UserId
        };
        _db.OtherIncomes.Add(income);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("OtherIncome.Create", nameof(OtherIncome), income.Id.ToString(), $"Amount={income.Amount:0.00}; Source={income.Source}");
        TempData["Success"] = "Other income recorded.";
        return RedirectToAction(nameof(OtherIncome), new { year = income.IncomeDate.Year, month = income.IncomeDate.Month });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOtherIncome(int id, string? reason, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A cancellation reason is required.";
            return RedirectToAction(nameof(OtherIncome));
        }

        var income = await _db.OtherIncomes.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == context.Value.SchoolId, cancellationToken);
        if (income is null) return NotFound();
        if (!income.IsCancelled)
        {
            income.IsCancelled = true;
            income.CancelledByUserId = context.Value.UserId;
            income.CancelledAtUtc = DateTime.UtcNow;
            income.CancellationReason = reason.Trim();
            await _db.SaveChangesAsync(cancellationToken);
            await _audit.WriteAsync("OtherIncome.Cancel", nameof(OtherIncome), income.Id.ToString(), income.CancellationReason);
        }
        TempData["Success"] = "Income entry cancelled; history was preserved.";
        return RedirectToAction(nameof(OtherIncome), new { year = income.IncomeDate.Year, month = income.IncomeDate.Month });
    }

    public async Task<IActionResult> FinancialSummary(int? year, int? month, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var (y, m) = NormalizePeriod(year, month);
        return View(await _finance.BuildFinancialSummaryAsync(context.Value.SchoolId, y, m, cancellationToken));
    }

    private async Task ValidateExpenseFormAsync(ExpenseFormViewModel model, int schoolId, CancellationToken cancellationToken)
    {
        if (model.ExpenseDate.Year < 2000 || model.ExpenseDate.Date > DateTime.Today.AddDays(1))
            ModelState.AddModelError(nameof(model.ExpenseDate), "Choose a valid expense date.");

        if (!await _db.ExpenseCategories.AnyAsync(x => x.Id == model.ExpenseCategoryId && x.SchoolId == schoolId && x.IsActive, cancellationToken))
            ModelState.AddModelError(nameof(model.ExpenseCategoryId), "Choose an active expense category.");
    }

    private async Task LoadCategoriesAsync(ExpenseFormViewModel model, int schoolId, CancellationToken cancellationToken)
    {
        model.Categories = await _db.ExpenseCategories.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    private static void ApplyApprovalRule(Expense expense)
    {
        expense.ApprovalRequired = expense.Amount >= HighValueApprovalThreshold;
        expense.ApprovalStatus = expense.ApprovalRequired ? ExpenseApprovalStatus.Pending : ExpenseApprovalStatus.NotRequired;
    }

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is null || string.IsNullOrWhiteSpace(user.Id)) return null;
        return (user.SchoolId.Value, user.Id);
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school before using expenses.";
        return RedirectToAction("Index", "SchoolSetup");
    }

    private static (int Year, int Month) NormalizePeriod(int? year, int? month)
    {
        var y = year is >= 2000 and <= 2200 ? year.Value : DateTime.Today.Year;
        var m = month is >= 1 and <= 12 ? month.Value : DateTime.Today.Month;
        return (y, m);
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
