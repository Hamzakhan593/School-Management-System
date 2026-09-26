using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class FeePayment
{
    public int Id { get; set; }
    public Guid? RequestId { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }

    [Required, StringLength(50)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime PaymentDateUtc { get; set; }
    public decimal Amount { get; set; }
    public FeePaymentMethod PaymentMethod { get; set; }

    [StringLength(150)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsReversed { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    [StringLength(450)]
    public string? ReversedByUserId { get; set; }
    [StringLength(500)]
    public string? ReversalReason { get; set; }

    [StringLength(450)]
    public string? ReceivedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<FeePaymentAllocation> Allocations { get; set; } = new List<FeePaymentAllocation>();
}
