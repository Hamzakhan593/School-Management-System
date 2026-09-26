namespace School_Management_System.Options;

public class FeeOptions
{
    public bool AutoGenerateEnabled { get; set; }
    public int MonthlyGenerationDay { get; set; } = 1;
    public int DefaultDueDay { get; set; } = 10;
    public decimal LateFeeFixedAmount { get; set; }
    public int LateFeeGraceDays { get; set; }
    public bool ApplyLateFeeOnCollection { get; set; } = true;
    public string SchoolTimeZoneId { get; set; } = "Asia/Karachi";
}
