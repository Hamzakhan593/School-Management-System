using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class FeeStructure
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }
    public int FeeHeadId { get; set; }

    public FeeStructureScope Scope { get; set; } = FeeStructureScope.Class;
    public int? SchoolClassId { get; set; }
    public int? StudentId { get; set; }
    public int? TermId { get; set; }

    public FeeFrequency Frequency { get; set; } = FeeFrequency.Monthly;
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EffectiveFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EffectiveTo { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ChargeDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public AcademicSession AcademicSession { get; set; } = null!;
    public FeeHead FeeHead { get; set; } = null!;
    public SchoolClass? SchoolClass { get; set; }
    public Student? Student { get; set; }
    public Term? Term { get; set; }
}
