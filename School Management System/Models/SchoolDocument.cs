using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class SchoolDocument
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public SchoolDocumentCategory Category { get; set; } = SchoolDocumentCategory.General;
    public NoticeAudience Audience { get; set; } = NoticeAudience.All;
    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFromUtc { get; set; }
    public DateTime? EffectiveUntilUtc { get; set; }

    [Required, StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    [StringLength(450)]
    public string? UploadedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public SchoolClass? SchoolClass { get; set; }
    public Section? Section { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
}
