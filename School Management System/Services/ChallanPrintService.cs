using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public sealed class ChallanPrintService(ApplicationDbContext db, ISystemSettingsService settings)
{
    public async Task<IReadOnlyList<ChallanPrintModel>> BuildAsync(int schoolId, IReadOnlyList<FeeChallan> selected, CancellationToken ct)
    {
        var ids = selected.Select(x => x.StudentId).Distinct().ToList();
        var unpaid = await db.FeeChallans.AsNoTracking().Where(x => x.SchoolId == schoolId && ids.Contains(x.StudentId)
            && !x.IsSuperseded && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived
            && x.CurrentChargesTotal > x.PaidAmount).ToListAsync(ct);
        var policy = await settings.GetAsync(schoolId, ct);
        return selected.Select(c =>
        {
            var prior = unpaid.Where(x => x.StudentId == c.StudentId && x.BillingPeriodStart < c.BillingPeriodStart).ToList();
            var late = policy.ApplyLateFeeOnCollection && c.LateFeeAmount == 0 && c.Balance > 0
                ? decimal.Round(policy.LateFeeFixedAmount, 2) : 0;
            return new ChallanPrintModel(c, prior, late, policy.LateFeeGraceDays);
        }).ToList();
    }
}
