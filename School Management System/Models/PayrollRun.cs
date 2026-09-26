using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class PayrollRun
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? ValidatedByUserId { get; set; }
    public DateTime? ValidatedAtUtc { get; set; }

    [StringLength(450)]
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }

    [StringLength(450)]
    public string? PostedByUserId { get; set; }
    public DateTime? PostedAtUtc { get; set; }

    public PayrollPaymentMethod? PaymentMethod { get; set; }

    [StringLength(150)]
    public string? PaymentReference { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ICollection<PayrollItem> Items { get; set; } = new List<PayrollItem>();
}
