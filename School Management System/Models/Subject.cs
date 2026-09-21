using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Subject
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    public bool HasTheory { get; set; } = true;
    public bool HasPractical { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? DefaultMaxMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? DefaultPassMarks { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
}
