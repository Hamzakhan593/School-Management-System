namespace School_Management_System.Models;

public class PayrollAdvanceDeduction
{
    public int Id { get; set; }
    public int PayrollItemId { get; set; }
    public int StaffAdvanceId { get; set; }
    public decimal Amount { get; set; }

    public PayrollItem PayrollItem { get; set; } = null!;
    public StaffAdvance StaffAdvance { get; set; } = null!;
}
