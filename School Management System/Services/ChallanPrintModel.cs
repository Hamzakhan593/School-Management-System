using System.Globalization;
using School_Management_System.Models;

namespace School_Management_System.Services;

public sealed record ChallanPrintModel(FeeChallan Challan, IReadOnlyList<FeeChallan> Arrears, decimal AdditionalLateFee, int GraceDays)
{
    public decimal ArrearsTotal => Arrears.Sum(x => x.Balance);
    public decimal Payable => Challan.Balance + ArrearsTotal;
    public string Months
    {
        get
        {
            var dates = Arrears.Select(x => x.BillingPeriodStart).Append(Challan.BillingPeriodStart)
                .Select(x => new DateTime(x.Year, x.Month, 1)).Distinct().Order().ToList();
            var ranges = new List<string>();
            for (var i = 0; i < dates.Count; i++)
            {
                var start = dates[i]; var end = start;
                while (i + 1 < dates.Count && dates[i + 1] == end.AddMonths(1)) end = dates[++i];
                ranges.Add(start == end ? start.ToString("MMM yyyy", CultureInfo.InvariantCulture)
                    : start.ToString("MMM yyyy", CultureInfo.InvariantCulture) + " to " + end.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            }
            return string.Join(", ", ranges);
        }
    }
}
