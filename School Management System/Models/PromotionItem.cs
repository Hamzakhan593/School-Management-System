using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class PromotionItem
{
    public long Id { get; set; }
    public int PromotionBatchId { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int SourceEnrollmentId { get; set; }
    public int? TargetEnrollmentId { get; set; }

    public int? SourceSchoolClassId { get; set; }
    public int? SourceSectionId { get; set; }
    public int? SourceAcademicGroupId { get; set; }

    public StudentStatus SourceStudentStatus { get; set; }
    public StudentEnrollmentStatus SourceEnrollmentStatus { get; set; }

    public PromotionDecision ProposedDecision { get; set; }
    public PromotionDecision? FinalDecision { get; set; }
    public PromotionItemStatus Status { get; set; } = PromotionItemStatus.Pending;

    public int? ProposedSchoolClassId { get; set; }
    public int? ProposedSectionId { get; set; }
    public int? ProposedAcademicGroupId { get; set; }

    public int? TargetSchoolClassId { get; set; }
    public int? TargetSectionId { get; set; }
    public int? TargetAcademicGroupId { get; set; }

    public int? LatestStudentResultId { get; set; }
    public bool? LatestResultPassed { get; set; }
    public decimal? LatestResultPercentage { get; set; }

    [StringLength(500)]
    public string? ReviewNote { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public PromotionBatch PromotionBatch { get; set; } = null!;
    public School School { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment SourceEnrollment { get; set; } = null!;
    public StudentEnrollment? TargetEnrollment { get; set; }
    public SchoolClass? SourceSchoolClass { get; set; }
    public Section? SourceSection { get; set; }
    public AcademicGroup? SourceAcademicGroup { get; set; }
    public SchoolClass? ProposedSchoolClass { get; set; }
    public Section? ProposedSection { get; set; }
    public AcademicGroup? ProposedAcademicGroup { get; set; }
    public SchoolClass? TargetSchoolClass { get; set; }
    public Section? TargetSection { get; set; }
    public AcademicGroup? TargetAcademicGroup { get; set; }
    public StudentResult? LatestStudentResult { get; set; }
}
