using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class ClassSubjectFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an academic session.")]
    [Display(Name = "Academic session")]
    public int AcademicSessionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a class.")]
    [Display(Name = "Class")]
    public int SchoolClassId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a subject.")]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Max marks override")]
    public decimal? MaxMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Pass marks override")]
    public decimal? PassMarks { get; set; }
}
