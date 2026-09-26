using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class FeeChallanBatch
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }

    [Required, StringLength(180)]
    public string BatchKey { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string BillingPeriod { get; set; } = string.Empty;

    public FeeBatchScope Scope { get; set; }
    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }

    [StringLength(1000)]
    public string? SelectedStudentIds { get; set; }

    public int ExpectedCount { get; set; }
    public int GeneratedCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public FeeBatchStatus Status { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public ICollection<FeeChallan> Challans { get; set; } = new List<FeeChallan>();
}
