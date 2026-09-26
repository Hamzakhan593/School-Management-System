using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class PromotionBatch
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int SourceAcademicSessionId { get; set; }
    public int TargetAcademicSessionId { get; set; }

    public PromotionBatchStatus Status { get; set; } = PromotionBatchStatus.Draft;

    public bool CopiedClassSubjects { get; set; }
    public bool CopiedFeeStructures { get; set; }
    public bool CopiedTeacherAssignments { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PreparedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public DateTime? RolledBackAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession SourceAcademicSession { get; set; } = null!;
    public AcademicSession TargetAcademicSession { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<PromotionItem> Items { get; set; } = new List<PromotionItem>();
}
