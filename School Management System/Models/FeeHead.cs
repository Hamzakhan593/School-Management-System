using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class FeeHead
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    public FeeFrequency DefaultFrequency { get; set; } = FeeFrequency.Monthly;

    public decimal DefaultAmount { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
}
