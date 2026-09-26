using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class StaffDocument
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public StaffDocumentType DocumentType { get; set; }

    [Required, StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public Staff Staff { get; set; } = null!;
}
