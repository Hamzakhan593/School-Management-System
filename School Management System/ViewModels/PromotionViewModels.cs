using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class PromotionIndexViewModel
{
    public IReadOnlyList<AcademicSession> Sessions { get; set; } = Array.Empty<AcademicSession>();
    public IReadOnlyList<PromotionBatchListItemViewModel> Batches { get; set; } = Array.Empty<PromotionBatchListItemViewModel>();
}

public class PromotionBatchListItemViewModel
{
    public int Id { get; set; }
    public string SourceSession { get; set; } = string.Empty;
    public string TargetSession { get; set; } = string.Empty;
    public PromotionBatchStatus Status { get; set; }
    public int StudentCount { get; set; }
    public int HoldCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class RolloverCreateViewModel
{
    [Required]
    [Display(Name = "Source academic session")]
    public int SourceAcademicSessionId { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "Next session name")]
    public string TargetSessionName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateTime TargetStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "End date")]
    public DateTime TargetEndDate { get; set; }

    [Range(1, 7)]
    [Display(Name = "Working days per week")]
    public int WorkingDaysPerWeek { get; set; } = 6;

    public bool CopyClassSubjects { get; set; } = true;
    public bool CopyFeeStructures { get; set; } = true;

    [Display(Name = "Teacher assignments to carry forward")]
    public List<int> SelectedTeacherAssignmentIds { get; set; } = new();

    [StringLength(1000)]
    public string? Notes { get; set; }

    public IReadOnlyList<AcademicSessionOptionViewModel> SourceSessions { get; set; } = Array.Empty<AcademicSessionOptionViewModel>();
    public IReadOnlyList<TeacherAssignmentOptionViewModel> TeacherAssignments { get; set; } = Array.Empty<TeacherAssignmentOptionViewModel>();
}

public class AcademicSessionOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AcademicSessionStatus Status { get; set; }
}

public class TeacherAssignmentOptionViewModel
{
    public int Id { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? SectionName { get; set; }
    public string SubjectName { get; set; } = string.Empty;
}

public class PromotionPreviewViewModel
{
    public int BatchId { get; set; }
    public string SourceSessionName { get; set; } = string.Empty;
    public string TargetSessionName { get; set; } = string.Empty;
    public PromotionBatchStatus BatchStatus { get; set; }
    public int TotalStudents { get; set; }
    public int PromoteCount { get; set; }
    public int RepeatCount { get; set; }
    public int HoldCount { get; set; }
    public int GraduateCount { get; set; }
    public int TransferOutCount { get; set; }
    public IReadOnlyList<PromotionClassOptionViewModel> Classes { get; set; } = Array.Empty<PromotionClassOptionViewModel>();
    public IReadOnlyList<PromotionSectionOptionViewModel> Sections { get; set; } = Array.Empty<PromotionSectionOptionViewModel>();
    public List<PromotionStudentRowViewModel> Items { get; set; } = new();
}

public class PromotionStudentRowViewModel
{
    public long PromotionItemId { get; set; }
    public int StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string SourceClass { get; set; } = string.Empty;
    public string? SourceSection { get; set; }
    public string? RollNumber { get; set; }
    public bool? LatestResultPassed { get; set; }
    public decimal? LatestResultPercentage { get; set; }
    public PromotionDecision ProposedDecision { get; set; }
    public PromotionDecision Decision { get; set; }
    public int? TargetSchoolClassId { get; set; }
    public int? TargetSectionId { get; set; }
    public string? ReviewNote { get; set; }
    public PromotionItemStatus Status { get; set; }
}

public class PromotionClassOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PromotionSectionOptionViewModel
{
    public int Id { get; set; }
    public int SchoolClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class PromotionPreviewPostViewModel
{
    public int BatchId { get; set; }
    public List<PromotionPreviewPostItemViewModel> Items { get; set; } = new();
}

public class PromotionPreviewPostItemViewModel
{
    public long PromotionItemId { get; set; }
    public PromotionDecision Decision { get; set; }
    public int? TargetSchoolClassId { get; set; }
    public int? TargetSectionId { get; set; }

    [StringLength(500)]
    public string? ReviewNote { get; set; }
}
