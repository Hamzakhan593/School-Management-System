using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Expense
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ExpenseCategoryId { get; set; }

    public DateTime ExpenseDate { get; set; }
    public decimal Amount { get; set; }

    [Required, StringLength(180)]
    public string PaidTo { get; set; } = string.Empty;

    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;

    [StringLength(150)]
    public string? ReferenceNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public ExpenseApprovalStatus ApprovalStatus { get; set; } = ExpenseApprovalStatus.NotRequired;
    public bool ApprovalRequired { get; set; }

    [StringLength(450)]
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    public bool IsCancelled { get; set; }
    [StringLength(450)]
    public string? CancelledByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    [StringLength(500)]
    public string? CancellationReason { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [StringLength(260)]
    public string? AttachmentOriginalName { get; set; }
    [StringLength(500)]
    public string? AttachmentStorageKey { get; set; }
    [StringLength(120)]
    public string? AttachmentContentType { get; set; }
    public long? AttachmentSizeBytes { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ExpenseCategory ExpenseCategory { get; set; } = null!;
}
