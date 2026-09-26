using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Options;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class FeeService : IFeeService
{
    private readonly ApplicationDbContext _db;
    private readonly FeeOptions _options;
    private readonly ISystemSettingsService _settings;

    public FeeService(ApplicationDbContext db, IOptions<FeeOptions> options, ISystemSettingsService settings)
    {
        _db = db;
        _options = options.Value;
        _settings = settings;
    }

    public async Task<FeeGenerationPreviewViewModel> PreviewGenerationAsync(
        int schoolId,
        ChallanGenerationViewModel request,
        CancellationToken cancellationToken = default)
    {
        var periodStart = NormalizeMonth(request.BillingMonth);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var billingPeriod = periodStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        var eligible = await GetEligibleStudentsAsync(schoolId, request, cancellationToken);
        var studentIds = eligible.Select(x => x.Student.Id).Distinct().ToList();
        var classIds = eligible.Where(x => x.Enrollment.SchoolClassId.HasValue)
            .Select(x => x.Enrollment.SchoolClassId!.Value).Distinct().ToList();
        var context = await LoadCalculationContextAsync(schoolId, request.AcademicSessionId, studentIds, classIds, cancellationToken);

        var existingStudentIds = await _db.FeeChallans.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                        && x.AcademicSessionId == request.AcademicSessionId
                        && x.BillingPeriod == billingPeriod
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && studentIds.Contains(x.StudentId))
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var existing = existingStudentIds.ToHashSet();

        var oldChallans = await _db.FeeChallans.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                        && studentIds.Contains(x.StudentId)
                        && x.BillingPeriod != billingPeriod
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .Select(x => new { x.StudentId, Outstanding = x.CurrentChargesTotal - x.PaidAmount })
            .ToListAsync(cancellationToken);
        var priorOutstanding = oldChallans
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Outstanding));

        var result = new FeeGenerationPreviewViewModel
        {
            EligibleStudents = eligible.Count,
            ExistingPreviousOutstanding = priorOutstanding.Values.Sum()
        };

        foreach (var entry in eligible.OrderBy(x => x.Student.FullName))
        {
            var row = new FeeGenerationPreviewRowViewModel
            {
                StudentId = entry.Student.Id,
                StudentName = entry.Student.FullName,
                AdmissionNumber = entry.Student.AdmissionNumber,
                ClassSection = FormatClassSection(entry.Enrollment),
                PreviousOutstanding = priorOutstanding.GetValueOrDefault(entry.Student.Id)
            };

            if (existing.Contains(entry.Student.Id))
            {
                row.Result = "Existing challan — skipped";
                result.WillSkipExisting++;
            }
            else
            {
                var lines = CalculateChargeLines(entry, periodStart, periodEnd, context);
                row.CurrentCharges = lines.Sum(x => x.NetAmount);
                if (lines.Count == 0 || row.CurrentCharges <= 0)
                {
                    row.Result = "No applicable fee structure";
                    result.WillSkipNoStructure++;
                }
                else
                {
                    row.Result = "Ready";
                    result.WillGenerate++;
                    result.EstimatedCurrentCharges += row.CurrentCharges;
                }
            }

            result.Rows.Add(row);
        }

        return result;
    }

    public async Task<FeeChallanBatch> GenerateAsync(
        int schoolId,
        string? userId,
        ChallanGenerationViewModel request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PreviewToken))
            throw new InvalidOperationException("Preview the challan batch before committing it.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var existingBatch = await _db.FeeChallanBatches
            .Include(x => x.Challans)
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.BatchKey == request.PreviewToken, cancellationToken);
        if (existingBatch is not null)
        {
            await tx.CommitAsync(cancellationToken);
            return existingBatch;
        }

        var periodStart = NormalizeMonth(request.BillingMonth);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var billingPeriod = periodStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var schoolSettings = await _settings.GetAsync(schoolId, cancellationToken);
        var dueDate = BuildDueDate(periodStart, schoolSettings.DefaultFeeDueDay);

        var eligible = await GetEligibleStudentsAsync(schoolId, request, cancellationToken);
        var studentIds = eligible.Select(x => x.Student.Id).Distinct().ToList();
        var classIds = eligible.Where(x => x.Enrollment.SchoolClassId.HasValue)
            .Select(x => x.Enrollment.SchoolClassId!.Value).Distinct().ToList();
        var context = await LoadCalculationContextAsync(schoolId, request.AcademicSessionId, studentIds, classIds, cancellationToken);

        var existingStudentIds = await _db.FeeChallans
            .Where(x => x.SchoolId == schoolId
                        && x.AcademicSessionId == request.AcademicSessionId
                        && x.BillingPeriod == billingPeriod
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && studentIds.Contains(x.StudentId))
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var existing = existingStudentIds.ToHashSet();

        var previousRows = await _db.FeeChallans
            .Where(x => x.SchoolId == schoolId
                        && studentIds.Contains(x.StudentId)
                        && x.BillingPeriod != billingPeriod
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .Select(x => new { x.StudentId, Outstanding = x.CurrentChargesTotal - x.PaidAmount })
            .ToListAsync(cancellationToken);
        var previous = previousRows.GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Outstanding));

        var batch = new FeeChallanBatch
        {
            SchoolId = schoolId,
            AcademicSessionId = request.AcademicSessionId,
            BatchKey = request.PreviewToken.Trim(),
            BillingPeriod = billingPeriod,
            Scope = request.Scope,
            SchoolClassId = request.SchoolClassId,
            SectionId = request.SectionId,
            SelectedStudentIds = request.SelectedStudentIds.Count == 0 ? null : string.Join(',', request.SelectedStudentIds.OrderBy(x => x)),
            ExpectedCount = eligible.Count,
            CreatedByUserId = userId,
            Status = FeeBatchStatus.Previewed
        };
        _db.FeeChallanBatches.Add(batch);

        foreach (var entry in eligible.OrderBy(x => x.Student.Id))
        {
            if (existing.Contains(entry.Student.Id))
            {
                batch.SkippedCount++;
                continue;
            }

            var lines = CalculateChargeLines(entry, periodStart, periodEnd, context);
            if (lines.Count == 0 || lines.Sum(x => x.NetAmount) <= 0)
            {
                batch.SkippedCount++;
                continue;
            }

            var challan = await CreateChallanEntityAsync(
                schoolId,
                request.AcademicSessionId,
                entry,
                billingPeriod,
                periodStart,
                periodEnd,
                dueDate,
                previous.GetValueOrDefault(entry.Student.Id),
                lines,
                userId,
                batch,
                version: 1,
                cancellationToken);

            batch.GeneratedCount++;
            batch.TotalAmount += challan.CurrentChargesTotal;
        }

        batch.Status = batch.SkippedCount > 0 ? FeeBatchStatus.CompletedWithSkips : FeeBatchStatus.Completed;
        batch.Notes = batch.GeneratedCount == 0
            ? "No new challans were created. Existing challans and/or missing structures were skipped."
            : $"Generated {batch.GeneratedCount}; skipped {batch.SkippedCount}.";

        if (string.IsNullOrWhiteSpace(userId))
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "FeeChallanBatch.AutoGenerate",
                EntityType = nameof(FeeChallanBatch),
                EntityId = batch.BatchKey,
                Details = $"Period={batch.BillingPeriod}; Generated={batch.GeneratedCount}; Skipped={batch.SkippedCount}; Total={batch.TotalAmount}"
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return batch;
    }

    public async Task<FeeChallan> RegenerateChallanAsync(
        int schoolId,
        int challanId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var old = await _db.FeeChallans
            .Include(x => x.Student)
            .Include(x => x.StudentEnrollment)
            .ThenInclude(x => x!.SchoolClass)
            .Include(x => x.StudentEnrollment)
            .ThenInclude(x => x!.Section)
            .FirstOrDefaultAsync(x => x.Id == challanId && x.SchoolId == schoolId, cancellationToken)
            ?? throw new InvalidOperationException("Challan was not found.");

        if (old.IsSuperseded || old.Status == FeeChallanStatus.Cancelled)
            throw new InvalidOperationException("This challan is already cancelled/superseded.");

        var hasActivePayment = await _db.FeePaymentAllocations
            .AnyAsync(x => x.FeeChallanId == old.Id && !x.FeePayment.IsReversed, cancellationToken);
        if (hasActivePayment)
            throw new InvalidOperationException("Reverse allocated payments before regenerating this challan.");

        var enrollment = old.StudentEnrollment ?? await _db.StudentEnrollments
            .Include(x => x.SchoolClass)
            .Include(x => x.Section)
            .FirstOrDefaultAsync(x => x.StudentId == old.StudentId && x.AcademicSessionId == old.AcademicSessionId && x.IsCurrent, cancellationToken)
            ?? throw new InvalidOperationException("Student enrollment for this session was not found.");

        var entry = new EligibleStudent(old.Student, enrollment);
        var classIds = enrollment.SchoolClassId.HasValue ? new List<int> { enrollment.SchoolClassId.Value } : new List<int>();
        var calcContext = await LoadCalculationContextAsync(schoolId, old.AcademicSessionId, new List<int> { old.StudentId }, classIds, cancellationToken);
        var lines = CalculateChargeLines(entry, old.BillingPeriodStart, old.BillingPeriodEnd, calcContext);
        if (lines.Count == 0 || lines.Sum(x => x.NetAmount) <= 0)
            throw new InvalidOperationException("No applicable fee structure exists for this student and billing period.");

        old.IsSuperseded = true;
        old.Status = FeeChallanStatus.Cancelled;
        old.CancellationReason = "Superseded by regenerated challan.";
        old.CancelledAtUtc = DateTime.UtcNow;
        old.CancelledByUserId = userId;
        old.UpdatedAtUtc = DateTime.UtcNow;

        var previousRows = await _db.FeeChallans
            .Where(x => x.SchoolId == schoolId
                        && x.StudentId == old.StudentId
                        && x.Id != old.Id
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .Select(x => x.CurrentChargesTotal - x.PaidAmount)
            .ToListAsync(cancellationToken);

        var batch = new FeeChallanBatch
        {
            SchoolId = schoolId,
            AcademicSessionId = old.AcademicSessionId,
            BatchKey = "REGEN-" + Guid.NewGuid().ToString("N"),
            BillingPeriod = old.BillingPeriod,
            Scope = FeeBatchScope.Individual,
            ExpectedCount = 1,
            GeneratedCount = 1,
            Status = FeeBatchStatus.Completed,
            SelectedStudentIds = old.StudentId.ToString(CultureInfo.InvariantCulture),
            CreatedByUserId = userId,
            Notes = $"Regenerated from challan {old.ChallanNumber}."
        };
        _db.FeeChallanBatches.Add(batch);

        var replacement = await CreateChallanEntityAsync(
            schoolId,
            old.AcademicSessionId,
            entry,
            old.BillingPeriod,
            old.BillingPeriodStart,
            old.BillingPeriodEnd,
            old.DueDate,
            previousRows.Sum(),
            lines,
            userId,
            batch,
            old.Version + 1,
            cancellationToken);
        batch.TotalAmount = replacement.CurrentChargesTotal;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return replacement;
    }

    public async Task CancelChallanAsync(
        int schoolId,
        int challanId,
        string reason,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cancellation reason is required.");

        var challan = await _db.FeeChallans
            .FirstOrDefaultAsync(x => x.Id == challanId && x.SchoolId == schoolId, cancellationToken)
            ?? throw new InvalidOperationException("Challan was not found.");

        if (challan.Status == FeeChallanStatus.Cancelled)
            return;

        var hasActivePayment = await _db.FeePaymentAllocations
            .AnyAsync(x => x.FeeChallanId == challanId && !x.FeePayment.IsReversed, cancellationToken);
        if (hasActivePayment)
            throw new InvalidOperationException("Reverse allocated payments before cancelling this challan.");

        challan.Status = FeeChallanStatus.Cancelled;
        challan.CancellationReason = reason.Trim();
        challan.CancelledAtUtc = DateTime.UtcNow;
        challan.CancelledByUserId = userId;
        challan.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeePayment> ReceivePaymentAsync(
        int schoolId,
        string? userId,
        PaymentEntryViewModel request,
        CancellationToken cancellationToken = default)
    {
        if (request.RequestId == Guid.Empty)
            throw new InvalidOperationException("Reopen the payment form before saving.");
        if (!Enum.IsDefined(request.PaymentMethod))
            throw new InvalidOperationException("Choose a valid payment method.");
        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        // Serialize retries for the same request across app instances, then return the original receipt.
        var lockName = $"fee-payment:{schoolId}:{request.RequestId}";
        await _db.Database.ExecuteSqlInterpolatedAsync($@"DECLARE @result int;
            EXEC @result = sp_getapplock @Resource={lockName}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
            IF @result < 0 THROW 51000, 'Payment is still being saved. Try again shortly.', 1;", cancellationToken);
        var previous = await _db.FeePayments.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.RequestId == request.RequestId, cancellationToken);
        if (previous is not null)
        {
            if (previous.StudentId != request.StudentId || previous.ReceivedByUserId != userId)
                throw new InvalidOperationException("This payment request has already been used.");
            await tx.CommitAsync(cancellationToken);
            return previous;
        }
        var student = await _db.Students
            .FirstOrDefaultAsync(x => x.Id == request.StudentId && x.SchoolId == schoolId, cancellationToken)
            ?? throw new InvalidOperationException("Student was not found.");

        await ApplyLateFeesForStudentAsync(schoolId, student.Id, cancellationToken);

        var challans = await _db.FeeChallans
            .Include(x => x.Items)
            .Include(x => x.PaymentAllocations)
                .ThenInclude(x => x.FeePayment)
            .Where(x => x.SchoolId == schoolId
                        && x.StudentId == student.Id
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var outstanding = challans.Sum(x => x.CurrentChargesTotal - x.PaidAmount);
        if (outstanding <= 0)
            throw new InvalidOperationException("This student has no outstanding fee balance.");
        if (request.Amount > outstanding + 0.005m)
            throw new InvalidOperationException($"Payment cannot exceed the current outstanding balance of {outstanding:N2}.");

        var payment = new FeePayment
        {
            RequestId = request.RequestId,
            SchoolId = schoolId,
            StudentId = student.Id,
            ReceiptNumber = await NextFinancialNumberAsync(schoolId, FinancialNumberType.Receipt, cancellationToken),
            PaymentDateUtc = DateTime.UtcNow,
            Amount = decimal.Round(request.Amount, 2),
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            ReceivedByUserId = userId
        };
        _db.FeePayments.Add(payment);

        var remaining = payment.Amount;
        foreach (var challan in challans)
        {
            if (remaining <= 0)
                break;

            var challanBalance = challan.CurrentChargesTotal - challan.PaidAmount;
            if (challanBalance <= 0)
                continue;

            var allocatedToChallan = 0m;
            foreach (var item in challan.Items.OrderBy(x => x.Id))
            {
                if (remaining <= 0 || allocatedToChallan >= challanBalance)
                    break;

                var alreadyPaidForItem = challan.PaymentAllocations
                    .Where(x => x.FeeChallanItemId == item.Id && !x.FeePayment.IsReversed)
                    .Sum(x => x.Amount);
                var itemBalance = Math.Max(0m, item.NetAmount - alreadyPaidForItem);
                if (itemBalance <= 0)
                    continue;

                var allocated = Math.Min(Math.Min(remaining, itemBalance), challanBalance - allocatedToChallan);
                payment.Allocations.Add(new FeePaymentAllocation
                {
                    FeeChallan = challan,
                    FeeChallanItem = item,
                    Amount = allocated
                });
                allocatedToChallan += allocated;
                remaining -= allocated;
            }

            // Fallback for legacy/mismatched item allocation history: never lose a valid payment.
            if (remaining > 0 && allocatedToChallan < challanBalance)
            {
                var fallback = Math.Min(remaining, challanBalance - allocatedToChallan);
                payment.Allocations.Add(new FeePaymentAllocation
                {
                    FeeChallan = challan,
                    Amount = fallback
                });
                allocatedToChallan += fallback;
                remaining -= fallback;
            }

            challan.PaidAmount += allocatedToChallan;
            challan.UpdatedAtUtc = DateTime.UtcNow;
            SetChallanStatus(challan, GetLocalToday());
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task ReversePaymentAsync(
        int schoolId,
        int paymentId,
        string reason,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Reversal reason is required.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var payment = await _db.FeePayments
            .Include(x => x.Allocations)
            .ThenInclude(x => x.FeeChallan)
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.SchoolId == schoolId, cancellationToken)
            ?? throw new InvalidOperationException("Payment was not found.");

        if (payment.IsReversed)
            throw new InvalidOperationException("This payment is already reversed.");

        foreach (var allocation in payment.Allocations)
        {
            var challan = allocation.FeeChallan;
            challan.PaidAmount = Math.Max(0m, challan.PaidAmount - allocation.Amount);
            challan.UpdatedAtUtc = DateTime.UtcNow;
            SetChallanStatus(challan, GetLocalToday());
        }

        payment.IsReversed = true;
        payment.ReversedAtUtc = DateTime.UtcNow;
        payment.ReversedByUserId = userId;
        payment.ReversalReason = reason.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task RefreshStatusesAndLateFeesAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        var challans = await _db.FeeChallans
            .Include(x => x.Items)
            .Where(x => x.SchoolId == schoolId
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .ToListAsync(cancellationToken);

        var schoolSettings = await _settings.GetAsync(schoolId, cancellationToken);
        var today = GetLocalToday(schoolSettings.SchoolTimeZoneId);
        foreach (var challan in challans)
        {
            if (ApplyLateFeeIfRequired(challan, today, schoolSettings))
                AddLateFeeAudit(challan);
            SetChallanStatus(challan, today);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetStudentOutstandingAsync(int schoolId, int studentId, CancellationToken cancellationToken = default)
    {
        var balances = await _db.FeeChallans.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                        && x.StudentId == studentId
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .Select(x => x.CurrentChargesTotal - x.PaidAmount)
            .ToListAsync(cancellationToken);
        return balances.Sum();
    }

    private async Task ApplyLateFeesForStudentAsync(int schoolId, int studentId, CancellationToken cancellationToken)
    {
        var schoolSettings = await _settings.GetAsync(schoolId, cancellationToken);
        if (!schoolSettings.ApplyLateFeeOnCollection || schoolSettings.LateFeeFixedAmount <= 0)
            return;

        var challans = await _db.FeeChallans
            .Include(x => x.Items)
            .Where(x => x.SchoolId == schoolId
                        && x.StudentId == studentId
                        && !x.IsSuperseded
                        && x.Status != FeeChallanStatus.Cancelled
                        && x.Status != FeeChallanStatus.Waived
                        && x.CurrentChargesTotal > x.PaidAmount)
            .ToListAsync(cancellationToken);
        var today = GetLocalToday(schoolSettings.SchoolTimeZoneId);
        foreach (var challan in challans)
        {
            if (ApplyLateFeeIfRequired(challan, today, schoolSettings))
                AddLateFeeAudit(challan);
            SetChallanStatus(challan, today);
        }
    }

    private static bool ApplyLateFeeIfRequired(FeeChallan challan, DateTime today, SystemSetting settings)
    {
        if (!settings.ApplyLateFeeOnCollection || settings.LateFeeFixedAmount <= 0 || challan.LateFeeAmount > 0)
            return false;

        if (today <= challan.DueDate.Date.AddDays(Math.Max(0, settings.LateFeeGraceDays)))
            return false;

        if (challan.CurrentChargesTotal - challan.PaidAmount <= 0)
            return false;

        var amount = decimal.Round(settings.LateFeeFixedAmount, 2);
        challan.Items.Add(new FeeChallanItem
        {
            Description = "Late Fee",
            Amount = amount,
            DiscountAmount = 0,
            NetAmount = amount,
            Notes = "Applied automatically according to configured late-fee policy."
        });
        challan.LateFeeAmount += amount;
        challan.Subtotal += amount;
        challan.CurrentChargesTotal += amount;
        challan.UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }

    private void AddLateFeeAudit(FeeChallan challan)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "FeeChallan.LateFeeApplied",
            EntityType = nameof(FeeChallan),
            EntityId = challan.Id.ToString(CultureInfo.InvariantCulture),
            Details = $"Challan={challan.ChallanNumber}; LateFee={challan.LateFeeAmount}; DueDate={challan.DueDate:yyyy-MM-dd}"
        });
    }

    private async Task<List<EligibleStudent>> GetEligibleStudentsAsync(
        int schoolId,
        ChallanGenerationViewModel request,
        CancellationToken cancellationToken)
    {
        var query = _db.StudentEnrollments.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.SchoolClass)
            .Include(x => x.Section)
            .Where(x => x.SchoolId == schoolId
                        && x.AcademicSessionId == request.AcademicSessionId
                        && x.IsCurrent
                        && x.Status == StudentEnrollmentStatus.Active
                        && x.Student.Status == StudentStatus.Active);

        query = request.Scope switch
        {
            FeeBatchScope.Individual when request.StudentId.HasValue
                => query.Where(x => x.StudentId == request.StudentId.Value),
            FeeBatchScope.ClassSection when request.SchoolClassId.HasValue
                => query.Where(x => x.SchoolClassId == request.SchoolClassId.Value
                                    && (!request.SectionId.HasValue || x.SectionId == request.SectionId.Value)),
            FeeBatchScope.SelectedStudents when request.SelectedStudentIds.Count > 0
                => query.Where(x => request.SelectedStudentIds.Contains(x.StudentId)),
            FeeBatchScope.WholeSchool => query,
            _ => query.Where(x => false)
        };

        var rows = await query.OrderBy(x => x.Student.FullName).ToListAsync(cancellationToken);
        return rows.Select(x => new EligibleStudent(x.Student, x)).ToList();
    }

    private async Task<FeeCalculationContext> LoadCalculationContextAsync(
        int schoolId,
        int academicSessionId,
        IReadOnlyCollection<int> studentIds,
        IReadOnlyCollection<int> classIds,
        CancellationToken cancellationToken)
    {
        var structures = await _db.FeeStructures.AsNoTracking()
            .Include(x => x.FeeHead)
            .Include(x => x.Term)
            .Where(x => x.SchoolId == schoolId
                        && x.AcademicSessionId == academicSessionId
                        && x.IsActive
                        && ((x.Scope == FeeStructureScope.Student && x.StudentId.HasValue && studentIds.Contains(x.StudentId.Value))
                            || (x.Scope == FeeStructureScope.Class && x.SchoolClassId.HasValue && classIds.Contains(x.SchoolClassId.Value))))
            .ToListAsync(cancellationToken);

        var discounts = await _db.StudentDiscounts.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive && studentIds.Contains(x.StudentId))
            .ToListAsync(cancellationToken);

        return new FeeCalculationContext(structures, discounts);
    }

    private static List<ChargeLine> CalculateChargeLines(
        EligibleStudent entry,
        DateTime periodStart,
        DateTime periodEnd,
        FeeCalculationContext context)
    {
        var relevant = context.Structures
            .Where(x => (x.Scope == FeeStructureScope.Student && x.StudentId == entry.Student.Id)
                        || (x.Scope == FeeStructureScope.Class && x.SchoolClassId == entry.Enrollment.SchoolClassId))
            .Where(x => IsStructureApplicable(x, periodStart, periodEnd))
            .GroupBy(x => x.FeeHeadId)
            .Select(g => g.OrderByDescending(x => x.Scope == FeeStructureScope.Student)
                          .ThenByDescending(x => x.Id)
                          .First())
            .OrderBy(x => x.FeeHead.SortOrder)
            .ThenBy(x => x.FeeHead.Name)
            .ToList();

        var discounts = context.Discounts
            .Where(x => x.StudentId == entry.Student.Id
                        && x.StartDate.Date <= periodEnd
                        && (!x.EndDate.HasValue || x.EndDate.Value.Date >= periodStart))
            .ToList();

        var lines = new List<ChargeLine>();
        foreach (var structure in relevant)
        {
            var amount = decimal.Round(structure.Amount, 2);
            if (amount <= 0)
                continue;

            decimal discountAmount = 0;
            foreach (var discount in discounts.Where(x => x.FeeHeadId == structure.FeeHeadId || (!x.FeeHeadId.HasValue && x.DiscountType == DiscountType.Percentage)))
            {
                var remaining = Math.Max(0m, amount - discountAmount);
                if (remaining <= 0) break;

                var calculated = discount.DiscountType == DiscountType.Percentage
                    ? amount * Math.Clamp(discount.Value, 0m, 100m) / 100m
                    : discount.Value;
                discountAmount += Math.Min(remaining, decimal.Round(calculated, 2));
            }

            lines.Add(new ChargeLine(structure.FeeHeadId, structure.FeeHead.Name, amount, discountAmount));
        }

        // A general fixed discount is a single challan-level amount, not repeated per fee head.
        var globalFixed = discounts
            .Where(x => !x.FeeHeadId.HasValue && x.DiscountType == DiscountType.FixedAmount)
            .Sum(x => x.Value);
        var remainingGlobal = decimal.Round(globalFixed, 2);
        for (var i = 0; i < lines.Count && remainingGlobal > 0; i++)
        {
            var line = lines[i];
            var available = Math.Max(0m, line.Amount - line.DiscountAmount);
            var apply = Math.Min(available, remainingGlobal);
            lines[i] = line with { DiscountAmount = line.DiscountAmount + apply };
            remainingGlobal -= apply;
        }

        return lines.Where(x => x.NetAmount > 0).ToList();
    }

    private static bool IsStructureApplicable(FeeStructure structure, DateTime periodStart, DateTime periodEnd)
    {
        if (structure.EffectiveFrom.HasValue && structure.EffectiveFrom.Value.Date > periodEnd)
            return false;
        if (structure.EffectiveTo.HasValue && structure.EffectiveTo.Value.Date < periodStart)
            return false;

        return structure.Frequency switch
        {
            FeeFrequency.Monthly => true,
            FeeFrequency.OneTime or FeeFrequency.Custom
                => structure.ChargeDate.HasValue
                   && structure.ChargeDate.Value.Date >= periodStart
                   && structure.ChargeDate.Value.Date <= periodEnd,
            FeeFrequency.TermBased
                => structure.Term is not null
                   && structure.Term.StartDate.Date >= periodStart
                   && structure.Term.StartDate.Date <= periodEnd,
            _ => false
        };
    }

    private async Task<FeeChallan> CreateChallanEntityAsync(
        int schoolId,
        int academicSessionId,
        EligibleStudent entry,
        string billingPeriod,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime dueDate,
        decimal previousOutstanding,
        IReadOnlyList<ChargeLine> lines,
        string? userId,
        FeeChallanBatch batch,
        int version,
        CancellationToken cancellationToken)
    {
        var subtotal = lines.Sum(x => x.Amount);
        var discount = lines.Sum(x => x.DiscountAmount);
        var total = lines.Sum(x => x.NetAmount);

        var challan = new FeeChallan
        {
            SchoolId = schoolId,
            AcademicSessionId = academicSessionId,
            StudentId = entry.Student.Id,
            StudentEnrollmentId = entry.Enrollment.Id,
            Batch = batch,
            ChallanNumber = await NextFinancialNumberAsync(schoolId, FinancialNumberType.Challan, cancellationToken),
            BillingPeriod = billingPeriod,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            IssueDate = GetLocalToday(),
            DueDate = dueDate,
            ClassNameSnapshot = entry.Enrollment.ClassName,
            SectionNameSnapshot = entry.Enrollment.SectionName,
            Subtotal = subtotal,
            DiscountTotal = discount,
            CurrentChargesTotal = total,
            PreviousOutstandingAtIssue = previousOutstanding,
            PaidAmount = 0,
            Status = FeeChallanStatus.Issued,
            Version = version,
            CreatedByUserId = userId
        };

        foreach (var line in lines)
        {
            challan.Items.Add(new FeeChallanItem
            {
                FeeHeadId = line.FeeHeadId,
                Description = line.Description,
                Amount = line.Amount,
                DiscountAmount = line.DiscountAmount,
                NetAmount = line.NetAmount
            });
        }

        _db.FeeChallans.Add(challan);
        return challan;
    }

    private async Task<string> NextFinancialNumberAsync(int schoolId, FinancialNumberType type, CancellationToken cancellationToken)
    {
        var schoolSettings = await _settings.GetAsync(schoolId, cancellationToken);
        var year = GetLocalToday(schoolSettings.SchoolTimeZoneId).Year;
        var counter = _db.FinancialNumberCounters.Local
            .FirstOrDefault(x => x.SchoolId == schoolId && x.NumberType == type && x.Year == year)
            ?? await _db.FinancialNumberCounters
                .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.NumberType == type && x.Year == year, cancellationToken);

        if (counter is null)
        {
            counter = new FinancialNumberCounter
            {
                SchoolId = schoolId,
                NumberType = type,
                Year = year,
                LastNumber = 0
            };
            _db.FinancialNumberCounters.Add(counter);
        }

        counter.LastNumber++;
        var prefix = type == FinancialNumberType.Challan ? schoolSettings.ChallanNumberPrefix : schoolSettings.ReceiptNumberPrefix;
        prefix = string.IsNullOrWhiteSpace(prefix) ? (type == FinancialNumberType.Challan ? "CH" : "RC") : prefix.Trim().ToUpperInvariant();
        var digits = Math.Clamp(schoolSettings.FinancialNumberDigits, 4, 10);
        return $"{prefix}-{year}-{counter.LastNumber.ToString(new string('0', digits), CultureInfo.InvariantCulture)}";
    }

    private static DateTime NormalizeMonth(DateTime date) => new(date.Year, date.Month, 1);

    private static DateTime BuildDueDate(DateTime month, int configuredDay)
    {
        var day = Math.Clamp(configuredDay, 1, DateTime.DaysInMonth(month.Year, month.Month));
        return new DateTime(month.Year, month.Month, day);
    }

    private DateTime GetLocalToday(string? timeZoneId = null)
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? _options.SchoolTimeZoneId : timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
        }
        catch
        {
            return DateTime.UtcNow.AddHours(5).Date;
        }
    }

    private static void SetChallanStatus(FeeChallan challan, DateTime today)
    {
        if (challan.Status is FeeChallanStatus.Cancelled or FeeChallanStatus.Waived)
            return;

        var balance = challan.CurrentChargesTotal - challan.PaidAmount;
        if (balance <= 0.005m)
            challan.Status = FeeChallanStatus.Paid;
        else if (challan.PaidAmount > 0)
            challan.Status = FeeChallanStatus.PartiallyPaid;
        else if (today.Date > challan.DueDate.Date)
            challan.Status = FeeChallanStatus.Overdue;
        else
            challan.Status = FeeChallanStatus.Issued;
    }

    private static string FormatClassSection(StudentEnrollment enrollment)
        => string.IsNullOrWhiteSpace(enrollment.SectionName)
            ? enrollment.ClassName
            : $"{enrollment.ClassName} / {enrollment.SectionName}";

    public static string CreateBatchToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToHexString(bytes);
    }

    private sealed record EligibleStudent(Student Student, StudentEnrollment Enrollment);
    private sealed record FeeCalculationContext(List<FeeStructure> Structures, List<StudentDiscount> Discounts);
    private sealed record ChargeLine(int FeeHeadId, string Description, decimal Amount, decimal DiscountAmount)
    {
        public decimal NetAmount => Math.Max(0m, Amount - DiscountAmount);
    }
}
