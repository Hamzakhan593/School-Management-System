namespace School_Management_System.Models;

public class FinancialNumberCounter
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public FinancialNumberType NumberType { get; set; }
    public int Year { get; set; }
    public int LastNumber { get; set; }
}
