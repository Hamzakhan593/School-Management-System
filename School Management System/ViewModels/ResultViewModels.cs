using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class ResultsIndexViewModel
{
    public int? SelectedSessionId { get; set; }
    public IReadOnlyList<AcademicSession> Sessions { get; set; } = [];
    public IReadOnlyList<Exam> Exams { get; set; } = [];
}

public class ClassResultViewModel
{
    public int MissingMarksCount { get; set; }
    public required Exam Exam { get; set; }
    public required SchoolClass SchoolClass { get; set; }
    public Section? Section { get; set; }
    public IReadOnlyList<Section> Sections { get; set; } = [];
    public IReadOnlyList<ClassResultRowViewModel> Rows { get; set; } = [];
    public bool HasPublishedResults { get; set; }
    public bool CanPublish { get; set; }
    public bool IsCorrectionInProgress { get; set; }
}

public class ClassResultRowViewModel
{
    public int StudentId { get; set; }
    public int StudentEnrollmentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal ObtainedMarks { get; set; }
    public decimal MaximumMarks { get; set; }
    public decimal Percentage { get; set; }
    public string Grade { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
    public int? Position { get; set; }
    public decimal? AttendancePercentage { get; set; }
    public int? PublishedVersion { get; set; }
}

public class ResultCardViewModel
{
    public int? SchoolPosition { get; set; }
    public int SchoolCandidateCount { get; set; }
    public int ClassCandidateCount { get; set; }
    public bool SchoolRankingComplete { get; set; }
    public required School School { get; set; }
    public required Exam Exam { get; set; }
    public required Student Student { get; set; }
    public required StudentEnrollment Enrollment { get; set; }
    public required StudentResult Result { get; set; }
    public IReadOnlyList<ResultCardSubjectRowViewModel> Subjects { get; set; } = [];
}

public class ResultCardSubjectRowViewModel
{
    public decimal WeightagePercent { get; set; } = 100m;
    public string Subject { get; set; } = string.Empty;
    public decimal MaximumMarks { get; set; }
    public decimal PassMarks { get; set; }
    public decimal? ObtainedMarks { get; set; }
    public MarkSpecialStatus SpecialStatus { get; set; }
    public bool Passed { get; set; }
    public string? Remarks { get; set; }
}

public class SubjectPerformanceViewModel
{
    public required Exam Exam { get; set; }
    public required SchoolClass SchoolClass { get; set; }
    public Section? Section { get; set; }
    public IReadOnlyList<Section> Sections { get; set; } = [];
    public IReadOnlyList<SubjectPerformanceRowViewModel> Subjects { get; set; } = [];
}

public class SubjectPerformanceRowViewModel
{
    public string Subject { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AverageMarks { get; set; }
    public decimal MaximumMarks { get; set; }
    public decimal AveragePercentage { get; set; }
    public decimal PassRate { get; set; }
}
