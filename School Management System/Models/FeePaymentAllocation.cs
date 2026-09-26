namespace School_Management_System.Models;

public class FeePaymentAllocation
{
    public int Id { get; set; }
    public int FeePaymentId { get; set; }
    public int FeeChallanId { get; set; }
    public int? FeeChallanItemId { get; set; }
    public decimal Amount { get; set; }

    public FeePayment FeePayment { get; set; } = null!;
    public FeeChallan FeeChallan { get; set; } = null!;
    public FeeChallanItem? FeeChallanItem { get; set; }
}
