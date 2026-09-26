using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class ExamIndexViewModel
{
    public int? SelectedSessionId { get; set; }
    public IReadOnlyList<AcademicSession> Sessions { get; set; } = [];
    public IReadOnlyList<Exam> Exams { get; set; } = [];
}

public class ExamFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int AcademicSessionId { get; set; }
    public int? TermId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string ExamType { get; set; } = "Term Exam";

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<AcademicSession> Sessions { get; set; } = [];
    public IReadOnlyList<Term> Terms { get; set; } = [];
}

public class ExamDetailsViewModel
{
    public Exam Exam { get; set; } = null!;
    public IReadOnlyList<SchoolClass> AvailableClasses { get; set; } = [];
    public IReadOnlyList<ClassSubject> AvailableClassSubjects { get; set; } = [];
    public IReadOnlyList<ExamMarksSheet> MarksSheets { get; set; } = [];
}

public class ExamSubjectFormViewModel
{
    [Required]
    public int ExamId { get; set; }
    [Required]
    public int SchoolClassId { get; set; }
    [Required]
    public int SubjectId { get; set; }

    [Range(typeof(decimal), "0.01", "100000")]
    public decimal MaxMarks { get; set; } = 100m;

    [Range(typeof(decimal), "0", "100000")]
    public decimal PassMarks { get; set; } = 40m;

    [Range(typeof(decimal), "0", "100000")]
    public decimal? TheoryMaxMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? PracticalMaxMarks { get; set; }

    [Range(typeof(decimal), "0.01", "1000")]
    public decimal WeightagePercent { get; set; } = 100m;
}

public class MarksEntryViewModel
{
    public int ExamId { get; set; }
    public int ExamSubjectId { get; set; }
    public int ExamMarksSheetId { get; set; }
    public int SchoolClassId { get; set; }
    public int? SectionId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? SectionName { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal PassMarks { get; set; }
    public decimal? TheoryMaxMarks { get; set; }
    public decimal? PracticalMaxMarks { get; set; }
    public ExamStatus ExamStatus { get; set; }
    public ExamMarksSheetStatus SheetStatus { get; set; }
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanVerify { get; set; }
    public bool CanLock { get; set; }
    public bool CanReopen { get; set; }
    public IReadOnlyList<Section> AvailableSections { get; set; } = [];
    public List<MarkEntryRowViewModel> Rows { get; set; } = [];
}

public class MarkEntryRowViewModel
{
    public int StudentId { get; set; }
    public int StudentEnrollmentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public MarkSpecialStatus SpecialStatus { get; set; }
    public decimal? TheoryMarks { get; set; }
    public decimal? PracticalMarks { get; set; }
    public string? TeacherRemarks { get; set; }
}
