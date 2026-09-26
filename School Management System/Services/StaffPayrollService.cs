using System.Data;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class StaffPayrollService : IStaffPayrollService
{
    private readonly ApplicationDbContext _db;

    public StaffPayrollService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PayrollIndexViewModel> BuildIndexAsync(int schoolId, int year, int month, CancellationToken cancellationToken = default)
    {
        (year, month) = NormalizePeriod(year, month);
        var runs = await _db.PayrollRuns.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.PeriodYear)
            .ThenByDescending(x => x.PeriodMonth)
            .Take(24)
            .ToListAsync(cancellationToken);

        var missing = await _db.Staff.AsNoTracking()
            .CountAsync(x => x.SchoolId == schoolId
                && (x.Status == StaffStatus.Active || x.Status == StaffStatus.OnLeave)
                && x.SalaryStructureId == null,
                cancellationToken);

        var pending = runs.Count(x => x.Status == PayrollRunStatus.Approved);
        var postedTotal = await _db.PayrollItems.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                && x.PayrollRun.PeriodYear == year
                && x.PayrollRun.PeriodMonth == month
                && x.PayrollRun.Status == PayrollRunStatus.Posted)
            .SumAsync(x => (decimal?)x.NetPay, cancellationToken) ?? 0m;

        return new PayrollIndexViewModel
        {
            Year = year,
            Month = month,
            Runs = runs,
            StaffWithoutSalaryStructure = missing,
            PendingApprovedRuns = pending,
            PostedPayrollThisMonth = postedTotal
        };
    }

    public async Task<SalaryStructureViewModel?> BuildSalaryStructureAsync(int schoolId, int staffId, CancellationToken cancellationToken = default)
    {
        var staff = await _db.Staff.AsNoTracking()
            .Include(x => x.SalaryStructure)
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == staffId, cancellationToken);
        if (staff is null) return null;

        var advances = await _db.StaffAdvances.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.StaffId == staffId)
            .OrderByDescending(x => x.Status == StaffAdvanceStatus.Active)
            .ThenByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
        var s = staff.SalaryStructure;

        return new SalaryStructureViewModel
        {
            StaffId = staff.Id,
            EmployeeId = staff.EmployeeId,
            StaffName = staff.FullName,
            Designation = staff.Designation,
            Department = staff.Department,
            SalaryStructureId = s?.Id,
            Name = s?.Name ?? $"{staff.EmployeeId} Salary",
            BasicSalary = s?.BasicSalary ?? 0,
            HouseAllowance = s?.HouseAllowance ?? 0,
            MedicalAllowance = s?.MedicalAllowance ?? 0,
            TransportAllowance = s?.TransportAllowance ?? 0,
            OtherAllowance = s?.OtherAllowance ?? 0,
            FixedDeduction = s?.FixedDeduction ?? 0,
            AbsenceDeductionPerDay = s?.AbsenceDeductionPerDay ?? 0,
            HalfDayDeductionPerDay = s?.HalfDayDeductionPerDay ?? 0,
            LateDeductionPerOccurrence = s?.LateDeductionPerOccurrence ?? 0,
            LeaveDeductionPerDay = s?.LeaveDeductionPerDay ?? 0,
            EffectiveFrom = s?.EffectiveFrom ?? DateTime.Today,
            IsActive = s?.IsActive ?? true,
            Advances = advances
        };
    }

    public async Task<PayrollOperationResult> SaveSalaryStructureAsync(int schoolId, SalaryStructureViewModel model, CancellationToken cancellationToken = default)
    {
        var staff = await _db.Staff.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == model.StaffId, cancellationToken);
        if (staff is null) return new(false, "Staff record was not found.");
        if (model.BasicSalary < 0 || model.HouseAllowance < 0 || model.MedicalAllowance < 0 || model.TransportAllowance < 0
            || model.OtherAllowance < 0 || model.FixedDeduction < 0 || model.AbsenceDeductionPerDay < 0
            || model.HalfDayDeductionPerDay < 0 || model.LateDeductionPerOccurrence < 0 || model.LeaveDeductionPerDay < 0)
            return new(false, "Salary amounts and deduction rates cannot be negative.");

        SalaryStructure? structure = null;
        if (staff.SalaryStructureId.HasValue)
        {
            structure = await _db.SalaryStructures.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == staff.SalaryStructureId.Value, cancellationToken);
        }

        if (structure is null)
        {
            structure = new SalaryStructure { SchoolId = schoolId, CreatedAtUtc = DateTime.UtcNow };
            _db.SalaryStructures.Add(structure);
        }

        structure.Name = string.IsNullOrWhiteSpace(model.Name) ? $"{staff.EmployeeId} Salary" : model.Name.Trim();
        structure.BasicSalary = model.BasicSalary;
        structure.HouseAllowance = model.HouseAllowance;
        structure.MedicalAllowance = model.MedicalAllowance;
        structure.TransportAllowance = model.TransportAllowance;
        structure.OtherAllowance = model.OtherAllowance;
        structure.FixedDeduction = model.FixedDeduction;
        structure.AbsenceDeductionPerDay = model.AbsenceDeductionPerDay;
        structure.HalfDayDeductionPerDay = model.HalfDayDeductionPerDay;
        structure.LateDeductionPerOccurrence = model.LateDeductionPerOccurrence;
        structure.LeaveDeductionPerDay = model.LeaveDeductionPerDay;
        structure.EffectiveFrom = model.EffectiveFrom.Date;
        structure.IsActive = model.IsActive;
        structure.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        if (staff.SalaryStructureId != structure.Id)
        {
            staff.SalaryStructureId = structure.Id;
            staff.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new(true, "Salary structure saved successfully.");
    }

    public async Task<PayrollOperationResult> AddAdvanceAsync(int schoolId, StaffAdvanceFormViewModel model, CancellationToken cancellationToken = default)
    {
        var staffExists = await _db.Staff.AsNoTracking().AnyAsync(x => x.SchoolId == schoolId && x.Id == model.StaffId, cancellationToken);
        if (!staffExists) return new(false, "Staff record was not found.");
        if (model.OriginalAmount <= 0 || model.MonthlyInstallment <= 0)
            return new(false, "Advance amount and monthly installment must be greater than zero.");
        if (model.MonthlyInstallment > model.OriginalAmount)
            return new(false, "Monthly installment cannot exceed the original advance/loan amount.");

        _db.StaffAdvances.Add(new StaffAdvance
        {
            SchoolId = schoolId,
            StaffId = model.StaffId,
            Type = model.Type,
            Description = model.Description.Trim(),
            OriginalAmount = model.OriginalAmount,
            OutstandingBalance = model.OriginalAmount,
            MonthlyInstallment = model.MonthlyInstallment,
            StartDate = model.StartDate.Date,
            Status = StaffAdvanceStatus.Active,
            Notes = Clean(model.Notes, 500),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Staff advance/loan added successfully.");
    }

    public async Task<PayrollOperationResult> CancelAdvanceAsync(int schoolId, int advanceId, CancellationToken cancellationToken = default)
    {
        var advance = await _db.StaffAdvances.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == advanceId, cancellationToken);
        if (advance is null) return new(false, "Advance/loan was not found.");
        if (advance.Status != StaffAdvanceStatus.Active) return new(false, "Only an active advance/loan can be cancelled.");

        advance.Status = StaffAdvanceStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Advance/loan cancelled. Historical payroll deductions remain unchanged.");
    }

    public async Task<PayrollOperationResult> GenerateRunAsync(int schoolId, string userId, int year, int month, CancellationToken cancellationToken = default)
    {
        (year, month) = NormalizePeriod(year, month);
        var existing = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.PeriodYear == year && x.PeriodMonth == month, cancellationToken);
        if (existing is not null)
            return new(false, $"A payroll run already exists for {new DateTime(year, month, 1):MMMM yyyy}.", existing.Id);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        existing = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.PeriodYear == year && x.PeriodMonth == month, cancellationToken);
        if (existing is not null)
            return new(false, "A payroll run for this month was created by another request.", existing.Id);

        var run = new PayrollRun
        {
            SchoolId = schoolId,
            PeriodYear = year,
            PeriodMonth = month,
            Status = PayrollRunStatus.Draft,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var staff = await _db.Staff
            .Include(x => x.SalaryStructure)
            .Where(x => x.SchoolId == schoolId
                && x.JoiningDate <= periodEnd
                && (!x.ExitDate.HasValue || x.ExitDate.Value >= periodStart)
                && x.Status != StaffStatus.Inactive)
            .OrderBy(x => x.Department)
            .ThenBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        var staffIds = staff.Select(x => x.Id).ToList();
        var attendance = await _db.StaffAttendances.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && staffIds.Contains(x.StaffId)
                && x.AttendanceDate >= periodStart && x.AttendanceDate <= periodEnd)
            .ToListAsync(cancellationToken);
        var attendanceByStaff = attendance.GroupBy(x => x.StaffId).ToDictionary(x => x.Key, x => x.ToList());

        var advances = await _db.StaffAdvances
            .Where(x => x.SchoolId == schoolId && staffIds.Contains(x.StaffId)
                && x.Status == StaffAdvanceStatus.Active && x.OutstandingBalance > 0 && x.StartDate <= periodEnd)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var advancesByStaff = advances.GroupBy(x => x.StaffId).ToDictionary(x => x.Key, x => x.ToList());

        foreach (var staffMember in staff)
        {
            var item = new PayrollItem
            {
                SchoolId = schoolId,
                PayrollRunId = run.Id,
                StaffId = staffMember.Id,
                EmployeeIdSnapshot = staffMember.EmployeeId,
                StaffNameSnapshot = staffMember.FullName,
                DepartmentSnapshot = staffMember.Department,
                ManualAllowance = 0,
                ManualDeduction = 0
            };
            ApplyCalculation(
                item,
                staffMember.SalaryStructure,
                attendanceByStaff.TryGetValue(staffMember.Id, out var a) ? a : new List<StaffAttendance>(),
                advancesByStaff.TryGetValue(staffMember.Id, out var adv) ? adv : new List<StaffAdvance>(),
                periodEnd);
            _db.PayrollItems.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, $"Payroll preview created for {periodStart:MMMM yyyy}.", run.Id);
    }

    public async Task<PayrollRunViewModel?> BuildRunAsync(int schoolId, int runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == runId, cancellationToken);
        if (run is null) return null;

        var items = await _db.PayrollItems.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.PayrollRunId == runId)
            .Include(x => x.Staff)
            .Include(x => x.AdvanceDeductions)
                .ThenInclude(x => x.StaffAdvance)
            .OrderBy(x => x.DepartmentSnapshot)
            .ThenBy(x => x.StaffNameSnapshot)
            .ToListAsync(cancellationToken);

        var departmentTotals = items
            .GroupBy(x => string.IsNullOrWhiteSpace(x.DepartmentSnapshot) ? "Unassigned" : x.DepartmentSnapshot!)
            .Select(g => new PayrollDepartmentSummaryViewModel
            {
                Department = g.Key,
                StaffCount = g.Count(),
                GrossPay = g.Sum(x => x.GrossPay),
                Deductions = g.Sum(x => x.TotalDeductions),
                NetPay = g.Sum(x => x.NetPay)
            })
            .OrderBy(x => x.Department)
            .ToList();

        return new PayrollRunViewModel
        {
            Run = run,
            Items = items,
            DepartmentTotals = departmentTotals,
            GrossTotal = items.Sum(x => x.GrossPay),
            DeductionTotal = items.Sum(x => x.TotalDeductions),
            NetTotal = items.Sum(x => x.NetPay),
            InvalidItems = items.Count(x => !x.IsValid)
        };
    }

    public async Task<PayrollOperationResult> UpdateAdjustmentAsync(int schoolId, PayrollAdjustmentViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.ManualAllowance < 0 || model.ManualDeduction < 0)
            return new(false, "Manual allowance/deduction cannot be negative.", model.RunId);

        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == model.RunId, cancellationToken);
        if (run is null) return new(false, "Payroll run was not found.");
        if (run.Status != PayrollRunStatus.Draft)
            return new(false, "Manual adjustments are allowed only while the payroll run is Draft.", run.Id);

        var item = await _db.PayrollItems.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.PayrollRunId == run.Id && x.Id == model.ItemId, cancellationToken);
        if (item is null) return new(false, "Payroll item was not found.", run.Id);

        item.ManualAllowance = model.ManualAllowance;
        item.ManualDeduction = model.ManualDeduction;
        item.ManualAdjustmentNote = Clean(model.ManualAdjustmentNote, 500);
        RecomputeTotals(item);
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Payroll adjustment updated.", run.Id);
    }

    public async Task<PayrollOperationResult> RecalculateAsync(int schoolId, int runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == runId, cancellationToken);
        if (run is null) return new(false, "Payroll run was not found.");
        if (run.Status != PayrollRunStatus.Draft)
            return new(false, "Only a Draft payroll run can be recalculated.", run.Id);

        await RecalculateRunInternalAsync(run, cancellationToken);
        return new(true, "Payroll preview recalculated from current salary, attendance and advance data.", run.Id);
    }

    public async Task<PayrollOperationResult> ValidateAsync(int schoolId, string userId, int runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == runId, cancellationToken);
        if (run is null) return new(false, "Payroll run was not found.");
        if (run.Status != PayrollRunStatus.Draft)
            return new(false, "Only a Draft payroll run can be validated.", run.Id);

        await RecalculateRunInternalAsync(run, cancellationToken);
        var invalid = await _db.PayrollItems.CountAsync(x => x.PayrollRunId == run.Id && !x.IsValid, cancellationToken);
        if (invalid > 0)
            return new(false, $"Validation failed. {invalid} payroll item(s) require correction, usually a missing/inactive salary structure or deductions greater than gross pay.", run.Id);

        run.Status = PayrollRunStatus.Validated;
        run.ValidatedByUserId = userId;
        run.ValidatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Payroll run validated successfully.", run.Id);
    }

    public async Task<PayrollOperationResult> ApproveAsync(int schoolId, string userId, int runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == runId, cancellationToken);
        if (run is null) return new(false, "Payroll run was not found.");
        if (run.Status != PayrollRunStatus.Validated)
            return new(false, "Only a Validated payroll run can be approved.", run.Id);

        run.Status = PayrollRunStatus.Approved;
        run.ApprovedByUserId = userId;
        run.ApprovedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Payroll run approved. It is now waiting for payment posting.", run.Id);
    }

    public async Task<PayrollOperationResult> PostAsync(int schoolId, string userId, PayrollPostViewModel model, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var run = await _db.PayrollRuns
            .Include(x => x.Items)
                .ThenInclude(x => x.AdvanceDeductions)
                    .ThenInclude(x => x.StaffAdvance)
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == model.RunId, cancellationToken);
        if (run is null) return new(false, "Payroll run was not found.");
        if (run.Status != PayrollRunStatus.Approved)
            return new(false, "Only an Approved payroll run can be posted.", run.Id);
        if (run.Items.Any(x => !x.IsValid))
            return new(false, "This payroll contains invalid items and cannot be posted.", run.Id);

        foreach (var item in run.Items)
        {
            foreach (var allocation in item.AdvanceDeductions)
            {
                if (allocation.StaffAdvance.Status != StaffAdvanceStatus.Active || allocation.StaffAdvance.OutstandingBalance < allocation.Amount)
                    return new(false, $"Advance/loan balance changed for staff ID {item.StaffId}. Recalculate and validate the payroll again before posting.", run.Id);
            }
        }

        foreach (var allocation in run.Items.SelectMany(x => x.AdvanceDeductions))
        {
            allocation.StaffAdvance.OutstandingBalance -= allocation.Amount;
            if (allocation.StaffAdvance.OutstandingBalance <= 0)
            {
                allocation.StaffAdvance.OutstandingBalance = 0;
                allocation.StaffAdvance.Status = StaffAdvanceStatus.Settled;
                allocation.StaffAdvance.SettledAtUtc = DateTime.UtcNow;
            }
        }

        run.Status = PayrollRunStatus.Posted;
        run.PaymentMethod = model.PaymentMethod;
        run.PaymentReference = Clean(model.PaymentReference, 150);
        run.PostedByUserId = userId;
        run.PostedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, "Payroll posted successfully. Payslips are now available.", run.Id);
    }

    public async Task<StaffPayrollHistoryViewModel?> BuildStaffHistoryAsync(int schoolId, int staffId, CancellationToken cancellationToken = default)
    {
        var staff = await _db.Staff.AsNoTracking()
            .Include(x => x.SalaryStructure)
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == staffId, cancellationToken);
        if (staff is null) return null;

        var advances = await _db.StaffAdvances.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.StaffId == staffId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
        var items = await _db.PayrollItems.AsNoTracking()
            .Include(x => x.PayrollRun)
            .Where(x => x.SchoolId == schoolId && x.StaffId == staffId && x.PayrollRun.Status == PayrollRunStatus.Posted)
            .OrderByDescending(x => x.PayrollRun.PeriodYear)
            .ThenByDescending(x => x.PayrollRun.PeriodMonth)
            .ToListAsync(cancellationToken);

        return new StaffPayrollHistoryViewModel
        {
            Staff = staff,
            SalaryStructure = staff.SalaryStructure,
            Advances = advances,
            PayrollItems = items
        };
    }

    private async Task RecalculateRunInternalAsync(PayrollRun run, CancellationToken cancellationToken)
    {
        var periodStart = new DateTime(run.PeriodYear, run.PeriodMonth, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var items = await _db.PayrollItems
            .Where(x => x.PayrollRunId == run.Id)
            .Include(x => x.AdvanceDeductions)
            .ToListAsync(cancellationToken);
        var staffIds = items.Select(x => x.StaffId).ToList();

        var staff = await _db.Staff
            .Include(x => x.SalaryStructure)
            .Where(x => x.SchoolId == run.SchoolId && staffIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var attendance = await _db.StaffAttendances.AsNoTracking()
            .Where(x => x.SchoolId == run.SchoolId && staffIds.Contains(x.StaffId)
                && x.AttendanceDate >= periodStart && x.AttendanceDate <= periodEnd)
            .ToListAsync(cancellationToken);
        var attendanceByStaff = attendance.GroupBy(x => x.StaffId).ToDictionary(x => x.Key, x => x.ToList());
        var advances = await _db.StaffAdvances
            .Where(x => x.SchoolId == run.SchoolId && staffIds.Contains(x.StaffId)
                && x.Status == StaffAdvanceStatus.Active && x.OutstandingBalance > 0 && x.StartDate <= periodEnd)
            .OrderBy(x => x.StartDate).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var advancesByStaff = advances.GroupBy(x => x.StaffId).ToDictionary(x => x.Key, x => x.ToList());

        _db.PayrollAdvanceDeductions.RemoveRange(items.SelectMany(x => x.AdvanceDeductions));
        foreach (var item in items)
        {
            if (!staff.TryGetValue(item.StaffId, out var staffMember))
            {
                item.IsValid = false;
                item.ValidationMessage = "Staff record no longer exists in this school.";
                continue;
            }

            item.EmployeeIdSnapshot = staffMember.EmployeeId;
            item.StaffNameSnapshot = staffMember.FullName;
            item.DepartmentSnapshot = staffMember.Department;
            ApplyCalculation(
                item,
                staffMember.SalaryStructure,
                attendanceByStaff.TryGetValue(item.StaffId, out var a) ? a : new List<StaffAttendance>(),
                advancesByStaff.TryGetValue(item.StaffId, out var adv) ? adv : new List<StaffAdvance>(),
                periodEnd);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyCalculation(
        PayrollItem item,
        SalaryStructure? structure,
        IReadOnlyCollection<StaffAttendance> attendance,
        IReadOnlyCollection<StaffAdvance> advances,
        DateTime periodEnd)
    {
        item.AdvanceDeductions.Clear();
        item.PresentDays = attendance.Count(x => x.Status == StaffAttendanceStatus.Present);
        item.AbsentDays = attendance.Count(x => x.Status == StaffAttendanceStatus.Absent);
        item.LateDays = attendance.Count(x => x.Status == StaffAttendanceStatus.Late);
        item.LeaveDays = attendance.Count(x => x.Status == StaffAttendanceStatus.Leave);
        item.HalfDays = attendance.Count(x => x.Status == StaffAttendanceStatus.HalfDay);

        if (structure is null || !structure.IsActive || structure.EffectiveFrom.Date > periodEnd)
        {
            item.SalaryStructureId = structure?.Id;
            item.BasicSalary = 0;
            item.FixedAllowances = 0;
            item.FixedDeduction = 0;
            item.AttendanceDeduction = 0;
            item.AdvanceDeduction = 0;
            item.IsValid = false;
            item.ValidationMessage = structure is null
                ? "Salary structure is missing."
                : "Salary structure is inactive or not effective for this payroll period.";
            RecomputeTotals(item);
            return;
        }

        item.SalaryStructureId = structure.Id;
        item.BasicSalary = structure.BasicSalary;
        item.FixedAllowances = structure.HouseAllowance + structure.MedicalAllowance + structure.TransportAllowance + structure.OtherAllowance;
        item.FixedDeduction = structure.FixedDeduction;
        item.AttendanceDeduction =
            (item.AbsentDays * structure.AbsenceDeductionPerDay)
            + (item.HalfDays * structure.HalfDayDeductionPerDay)
            + (item.LateDays * structure.LateDeductionPerOccurrence)
            + (item.LeaveDays * structure.LeaveDeductionPerDay);

        var advanceTotal = 0m;
        foreach (var advance in advances.Where(x => x.Status == StaffAdvanceStatus.Active && x.StartDate <= periodEnd && x.OutstandingBalance > 0))
        {
            var deduction = Math.Min(advance.MonthlyInstallment, advance.OutstandingBalance);
            if (deduction <= 0) continue;
            item.AdvanceDeductions.Add(new PayrollAdvanceDeduction
            {
                StaffAdvanceId = advance.Id,
                StaffAdvance = advance,
                Amount = deduction
            });
            advanceTotal += deduction;
        }
        item.AdvanceDeduction = advanceTotal;
        item.IsValid = true;
        item.ValidationMessage = null;
        RecomputeTotals(item);
        if (item.TotalDeductions > item.GrossPay)
        {
            item.IsValid = false;
            item.ValidationMessage = "Total deductions exceed gross pay. Adjust the salary/advance/manual deductions before validation.";
            item.NetPay = 0;
        }
    }

    private static void RecomputeTotals(PayrollItem item)
    {
        item.GrossPay = item.BasicSalary + item.FixedAllowances + item.ManualAllowance;
        item.TotalDeductions = item.AttendanceDeduction + item.FixedDeduction + item.AdvanceDeduction + item.ManualDeduction;
        item.NetPay = Math.Max(0, item.GrossPay - item.TotalDeductions);
        if (item.TotalDeductions > item.GrossPay)
        {
            item.IsValid = false;
            item.ValidationMessage = "Total deductions exceed gross pay.";
        }
    }

    private static (int Year, int Month) NormalizePeriod(int year, int month)
    {
        var now = DateTime.Today;
        if (year < 2000 || year > 2200) year = now.Year;
        if (month < 1 || month > 12) month = now.Month;
        return (year, month);
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
