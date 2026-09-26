using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class CommunicationHistory
{
    public long Id { get; set; }
    public int SchoolId { get; set; }
    public int? NoticeId { get; set; }

    public CommunicationChannel Channel { get; set; }
    public NoticeAudience Audience { get; set; }
    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }

    [StringLength(200)]
    public string? Subject { get; set; }

    [Required, StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    public int RecipientCount { get; set; }
    public CommunicationDeliveryStatus Status { get; set; } = CommunicationDeliveryStatus.Draft;

    [StringLength(120)]
    public string? ProviderName { get; set; }

    [StringLength(1000)]
    public string? StatusDetail { get; set; }

    public DateTime? SentAtUtc { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public School School { get; set; } = null!;
    public Notice? Notice { get; set; }
    public SchoolClass? SchoolClass { get; set; }
    public Section? Section { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}
