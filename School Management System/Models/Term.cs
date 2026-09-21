using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class Term
{
    public int Id { get; set; }
    public int AcademicSessionId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public int DisplayOrder { get; set; }

    public AcademicSession AcademicSession { get; set; } = null!;
}
