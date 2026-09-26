using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StaffNumberCounter
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int Year { get; set; }
    public int LastNumber { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
