using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class GradingRule
{
    public int Id { get; set; }
    public int GradingSchemeId { get; set; }

    [Required, StringLength(20)]
    public string Grade { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal MinPercentage { get; set; }

    [Range(0, 100)]
    public decimal MaxPercentage { get; set; }

    [StringLength(150)]
    public string? Remarks { get; set; }

    public GradingScheme GradingScheme { get; set; } = null!;
}
