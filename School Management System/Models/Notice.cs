using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Notice
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(6000)]
    public string Body { get; set; } = string.Empty;

    public NoticeAudience Audience { get; set; } = NoticeAudience.All;
    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }

    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? PublishFromUtc { get; set; }
    public DateTime? PublishUntilUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    [StringLength(260)]
    public string? AttachmentOriginalName { get; set; }

    [StringLength(500)]
    public string? AttachmentStorageKey { get; set; }

    [StringLength(120)]
    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public SchoolClass? SchoolClass { get; set; }
    public Section? Section { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<CommunicationHistory> CommunicationHistory { get; set; } = new List<CommunicationHistory>();
}
