using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class OtherIncome
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public DateTime IncomeDate { get; set; }
    public decimal Amount { get; set; }

    [Required, StringLength(180)]
    public string Source { get; set; } = string.Empty;

    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;

    [StringLength(150)]
    public string? ReferenceNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool IsCancelled { get; set; }
    [StringLength(450)]
    public string? CancelledByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    [StringLength(500)]
    public string? CancellationReason { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
}
