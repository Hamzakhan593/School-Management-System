using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class AdmissionDocument
{
    public int Id { get; set; }
    public int AdmissionApplicationId { get; set; }
    public AdmissionDocumentType DocumentType { get; set; }

    [Required, StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public AdmissionApplication AdmissionApplication { get; set; } = null!;
}
