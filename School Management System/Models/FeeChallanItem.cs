using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class FeeChallanItem
{
    public int Id { get; set; }
    public int FeeChallanId { get; set; }
    public int? FeeHeadId { get; set; }

    [Required, StringLength(120)]
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    public FeeChallan FeeChallan { get; set; } = null!;
    public FeeHead? FeeHead { get; set; }
    public ICollection<FeePaymentAllocation> PaymentAllocations { get; set; } = new List<FeePaymentAllocation>();
}
