using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IAttendanceService
{
    Task<AttendanceMarkingViewModel> BuildMarkingSheetAsync(
        int schoolId,
        string currentUserId,
        bool isManager,
        int? academicSessionId,
        int? schoolClassId,
        int? sectionId,
        DateTime attendanceDate,
        CancellationToken cancellationToken = default);

    Task<AttendanceSaveResult> SaveClassAttendanceAsync(
        int schoolId,
        string currentUserId,
        bool isManager,
        AttendanceMarkingViewModel model,
        CancellationToken cancellationToken = default);

    Task<MonthlyAttendanceReportViewModel> BuildMonthlyReportAsync(
        int schoolId,
        string currentUserId,
        bool isManager,
        int? academicSessionId,
        int? schoolClassId,
        int? sectionId,
        DateTime month,
        CancellationToken cancellationToken = default);
}

public record AttendanceSaveResult(
    bool Success,
    string Message,
    int CreatedCount = 0,
    int UpdatedCount = 0,
    bool WasLateCorrection = false);
