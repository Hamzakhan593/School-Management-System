using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models;

public class FeeChallan
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int StudentId { get; set; }
    public int? StudentEnrollmentId { get; set; }
    public int? FeeChallanBatchId { get; set; }

    [Required, StringLength(50)]
    public string ChallanNumber { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string BillingPeriod { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime BillingPeriodStart { get; set; }

    [DataType(DataType.Date)]
    public DateTime BillingPeriodEnd { get; set; }

    [DataType(DataType.Date)]
    public DateTime IssueDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; }

    [StringLength(80)]
    public string? ClassNameSnapshot { get; set; }

    [StringLength(80)]
    public string? SectionNameSnapshot { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal CurrentChargesTotal { get; set; }
    public decimal PreviousOutstandingAtIssue { get; set; }
    public decimal PaidAmount { get; set; }

    public FeeChallanStatus Status { get; set; } = FeeChallanStatus.Issued;

    public int Version { get; set; } = 1;
    public bool IsSuperseded { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    [StringLength(450)]
    public string? CancelledByUserId { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [NotMapped]
    public decimal Balance => Math.Max(0m, CurrentChargesTotal - PaidAmount);

    [NotMapped]
    public decimal TotalDueAtIssue => CurrentChargesTotal + PreviousOutstandingAtIssue;

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment? StudentEnrollment { get; set; }
    public FeeChallanBatch? Batch { get; set; }
    public ICollection<FeeChallanItem> Items { get; set; } = new List<FeeChallanItem>();
    public ICollection<FeePaymentAllocation> PaymentAllocations { get; set; } = new List<FeePaymentAllocation>();
}
