using School_Management_System.Models;

namespace School_Management_System.Services;

public interface IStudentService
{
    Task<(bool Success, string Message, StudentEnrollment? Enrollment)> ChangePlacementAsync(
        int studentId,
        int schoolId,
        int academicSessionId,
        int schoolClassId,
        int? sectionId,
        int? academicGroupId,
        string? rollNumber,
        DateTime effectiveFrom,
        StudentEnrollmentStatus status,
        string? notes,
        CancellationToken cancellationToken = default);
}
