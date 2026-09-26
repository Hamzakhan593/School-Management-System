using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Principal + "," + AppRoles.Admin + "," + AppRoles.Accountant + "," + AppRoles.HR)]
public class PayrollController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;
    private readonly IStaffPayrollService _payroll;
    private readonly IStaffPayrollPdfService _pdf;
    private readonly IAuditService _audit;

    public PayrollController(
        ApplicationDbContext db,
        ISchoolContextService schoolContext,
        IStaffPayrollService payroll,
        IStaffPayrollPdfService pdf,
        IAuditService audit)
    {
        _db = db;
        _schoolContext = schoolContext;
        _payroll = payroll;
        _pdf = pdf;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var now = DateTime.Today;
        var model = await _payroll.BuildIndexAsync(context.Value.SchoolId, year ?? now.Year, month ?? now.Month, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SalaryStructure(int staffId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await _payroll.BuildSalaryStructureAsync(context.Value.SchoolId, staffId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalaryStructure(SalaryStructureViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!ModelState.IsValid)
        {
            var rebuilt = await _payroll.BuildSalaryStructureAsync(context.Value.SchoolId, model.StaffId, cancellationToken);
            if (rebuilt is null) return NotFound();
            rebuilt.Name = model.Name;
            rebuilt.BasicSalary = model.BasicSalary;
            rebuilt.HouseAllowance = model.HouseAllowance;
            rebuilt.MedicalAllowance = model.MedicalAllowance;
            rebuilt.TransportAllowance = model.TransportAllowance;
            rebuilt.OtherAllowance = model.OtherAllowance;
            rebuilt.FixedDeduction = model.FixedDeduction;
            rebuilt.AbsenceDeductionPerDay = model.AbsenceDeductionPerDay;
            rebuilt.HalfDayDeductionPerDay = model.HalfDayDeductionPerDay;
            rebuilt.LateDeductionPerOccurrence = model.LateDeductionPerOccurrence;
            rebuilt.LeaveDeductionPerDay = model.LeaveDeductionPerDay;
            rebuilt.EffectiveFrom = model.EffectiveFrom;
            rebuilt.IsActive = model.IsActive;
            return View(rebuilt);
        }

        var result = await _payroll.SaveSalaryStructureAsync(context.Value.SchoolId, model, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.SalaryStructureSaved", "Staff", model.StaffId.ToString(), $"BasicSalary={model.BasicSalary:0.00}");
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(SalaryStructure), new { staffId = model.StaffId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAdvance(StaffAdvanceFormViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Complete the advance/loan form with valid amounts.";
            return RedirectToAction(nameof(SalaryStructure), new { staffId = model.StaffId });
        }
        var result = await _payroll.AddAdvanceAsync(context.Value.SchoolId, model, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.AdvanceAdded", "Staff", model.StaffId.ToString(), $"Type={model.Type}; Amount={model.OriginalAmount:0.00}; Installment={model.MonthlyInstallment:0.00}");
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(SalaryStructure), new { staffId = model.StaffId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAdvance(int id, int staffId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _payroll.CancelAdvanceAsync(context.Value.SchoolId, id, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.AdvanceCancelled", "StaffAdvance", id.ToString());
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(SalaryStructure), new { staffId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int year, int month, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _payroll.GenerateRunAsync(context.Value.SchoolId, context.Value.UserId, year, month, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.RunGenerated", "PayrollRun", result.RunId?.ToString(), $"Year={year}; Month={month}");
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return result.RunId.HasValue ? RedirectToAction(nameof(Run), new { id = result.RunId.Value }) : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Run(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await _payroll.BuildRunAsync(context.Value.SchoolId, id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAdjustment(PayrollAdjustmentViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Manual adjustments must be valid non-negative amounts.";
            return RedirectToAction(nameof(Run), new { id = model.RunId });
        }
        var result = await _payroll.UpdateAdjustmentAsync(context.Value.SchoolId, model, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.ManualAdjustmentUpdated", "PayrollItem", model.ItemId.ToString(), $"Allowance={model.ManualAllowance:0.00}; Deduction={model.ManualDeduction:0.00}; Note={model.ManualAdjustmentNote}");
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Run), new { id = model.RunId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _payroll.RecalculateAsync(context.Value.SchoolId, id, cancellationToken);
        if (result.Success) TempData["Success"] = result.Message; else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Run), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateRun(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _payroll.ValidateAsync(context.Value.SchoolId, context.Value.UserId, id, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.RunValidated", "PayrollRun", id.ToString());
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Run), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        if (!(User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal) || User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.HR)))
            return Forbid();
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var result = await _payroll.ApproveAsync(context.Value.SchoolId, context.Value.UserId, id, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.RunApproved", "PayrollRun", id.ToString());
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Run), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(PayrollPostViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Select a payment method before posting payroll.";
            return RedirectToAction(nameof(Run), new { id = model.RunId });
        }
        var result = await _payroll.PostAsync(context.Value.SchoolId, context.Value.UserId, model, cancellationToken);
        if (result.Success)
        {
            await _audit.WriteAsync("Payroll.RunPosted", "PayrollRun", model.RunId.ToString(), $"Method={model.PaymentMethod}; Ref={model.PaymentReference}");
            TempData["Success"] = result.Message;
        }
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Run), new { id = model.RunId });
    }

    [HttpGet]
    public async Task<IActionResult> Payslip(int itemId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var item = await _db.PayrollItems.AsNoTracking()
            .Include(x => x.PayrollRun)
            .FirstOrDefaultAsync(x => x.SchoolId == context.Value.SchoolId && x.Id == itemId, cancellationToken);
        if (item is null) return NotFound();
        if (item.PayrollRun.Status != PayrollRunStatus.Posted)
        {
            TempData["Error"] = "Payslips are available after payroll is posted.";
            return RedirectToAction(nameof(Run), new { id = item.PayrollRunId });
        }
        var school = await _db.Schools.AsNoTracking().FirstAsync(x => x.Id == context.Value.SchoolId, cancellationToken);
        var bytes = await _pdf.CreatePayslipPdfAsync(school, item, cancellationToken);
        var filename = $"Payslip-{item.EmployeeIdSnapshot}-{item.PayrollRun.PeriodYear}-{item.PayrollRun.PeriodMonth:00}.pdf";
        return File(bytes, "application/pdf", filename);
    }

    [HttpGet]
    public async Task<IActionResult> StaffHistory(int staffId, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync();
        if (context is null) return RedirectToSchoolSetup();
        var model = await _payroll.BuildStaffHistoryAsync(context.Value.SchoolId, staffId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    private async Task<(int SchoolId, string UserId)?> GetContextAsync()
    {
        var user = await _schoolContext.GetCurrentUserAsync();
        if (user?.SchoolId is null || string.IsNullOrWhiteSpace(user.Id)) return null;
        return (user.SchoolId.Value, user.Id);
    }

    private IActionResult RedirectToSchoolSetup()
    {
        TempData["Error"] = "Complete School Profile and link your user to the school before using payroll.";
        return RedirectToAction("Index", "SchoolSetup");
    }
}
