using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class School
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? RegistrationNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [StringLength(150)]
    public string? PrincipalName { get; set; }

    [StringLength(300)]
    public string? LogoPath { get; set; }

    [StringLength(500)]
    public string? ChallanFooterText { get; set; }

    [StringLength(500)]
    public string? ReceiptFooterText { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<AcademicSession> AcademicSessions { get; set; } = new List<AcademicSession>();
}
