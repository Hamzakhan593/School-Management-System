using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StudentDiscount
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int? FeeHeadId { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Required, StringLength(300)]
    public string ApprovalNote { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public School School { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public FeeHead? FeeHead { get; set; }
}
