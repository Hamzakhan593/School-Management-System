using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class TermCreateViewModel
{
    public int AcademicSessionId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Range(0, 100)]
    public int DisplayOrder { get; set; }
}
