using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class GradingScheme
{
    public int Id { get; set; }
    public int AcademicSessionId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "Default Grading Scheme";

    public bool IsDefault { get; set; } = true;

    public AcademicSession AcademicSession { get; set; } = null!;
    public ICollection<GradingRule> Rules { get; set; } = new List<GradingRule>();
}
