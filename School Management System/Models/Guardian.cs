using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Guardian
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Relationship { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Occupation { get; set; }

    [StringLength(30)]
    public string? Cnic { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
}
