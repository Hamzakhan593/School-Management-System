using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AttendanceMarkingViewModel
{
    [Required]
    public int AcademicSessionId { get; set; }

    [Required]
    public int SchoolClassId { get; set; }

    public int? SectionId { get; set; }

    [DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? CorrectionReason { get; set; }

    public bool CanEdit { get; set; }
    public bool RequiresCorrectionReason { get; set; }
    public bool IsManager { get; set; }
    public string? AccessMessage { get; set; }
    public string? SelectedSessionName { get; set; }
    public string? SelectedClassName { get; set; }
    public string? SelectedSectionName { get; set; }

    public List<AcademicSession> Sessions { get; set; } = [];
    public List<SchoolClass> Classes { get; set; } = [];
    public List<Section> Sections { get; set; } = [];
    public List<AttendanceStudentRowViewModel> Students { get; set; } = [];
}

public class AttendanceStudentRowViewModel
{
    public int StudentId { get; set; }
    public int StudentEnrollmentId { get; set; }
    public int? AttendanceId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public StudentAttendanceStatus Status { get; set; } = StudentAttendanceStatus.Present;
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;

    [StringLength(300)]
    public string? Remarks { get; set; }

    public bool AlreadySaved { get; set; }
}
