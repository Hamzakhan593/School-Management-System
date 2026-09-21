using System.Data;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _db;

    public StudentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Message, StudentEnrollment? Enrollment)> ChangePlacementAsync(
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
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var student = await _db.Students.FirstOrDefaultAsync(x => x.Id == studentId && x.SchoolId == schoolId, cancellationToken);
        if (student is null) return (false, "Student was not found.", null);

        var session = await _db.AcademicSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == academicSessionId && x.SchoolId == schoolId, cancellationToken);
        if (session is null) return (false, "Academic session was not found for this school.", null);

        if (effectiveFrom.Date < session.StartDate.Date || effectiveFrom.Date > session.EndDate.Date)
            return (false, $"Effective date must be inside academic session {session.Name}.", null);

        var schoolClass = await _db.SchoolClasses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schoolClassId && x.SchoolId == schoolId && x.IsActive, cancellationToken);
        if (schoolClass is null) return (false, "Select a valid active class.", null);

        Section? section = null;
        if (sectionId.HasValue)
        {
            section = await _db.Sections.AsNoTracking().FirstOrDefaultAsync(x =>
                x.Id == sectionId.Value && x.SchoolId == schoolId && x.SchoolClassId == schoolClassId && x.IsActive, cancellationToken);
            if (section is null) return (false, "Selected section does not belong to the selected class.", null);
        }

        AcademicGroup? group = null;
        if (academicGroupId.HasValue)
        {
            group = await _db.AcademicGroups.AsNoTracking().FirstOrDefaultAsync(x =>
                x.Id == academicGroupId.Value && x.SchoolId == schoolId && x.SchoolClassId == schoolClassId && x.IsActive, cancellationToken);
            if (group is null) return (false, "Selected group/stream does not belong to the selected class.", null);
        }

        if (section is not null)
        {
            var currentCount = await _db.StudentEnrollments.CountAsync(x =>
                x.SchoolId == schoolId && x.AcademicSessionId == academicSessionId && x.SectionId == section.Id && x.IsCurrent && x.StudentId != studentId,
                cancellationToken);
            if (currentCount >= section.Capacity)
                return (false, $"Section {section.Name} is already at its configured capacity ({section.Capacity}).", null);
        }

        var current = await _db.StudentEnrollments
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.SchoolId == schoolId && x.IsCurrent, cancellationToken);

        if (current is not null)
        {
            if (effectiveFrom.Date <= current.EffectiveFrom.Date)
                return (false, "New placement date must be after the current placement start date so history remains valid.", null);

            current.IsCurrent = false;
            current.EffectiveTo = effectiveFrom.Date.AddDays(-1);
            if (current.Status == StudentEnrollmentStatus.Active)
                current.Status = current.AcademicSessionId == academicSessionId
                    ? StudentEnrollmentStatus.Transferred
                    : StudentEnrollmentStatus.Promoted;
            current.UpdatedAtUtc = DateTime.UtcNow;
        }

        var enrollment = new StudentEnrollment
        {
            SchoolId = schoolId,
            StudentId = studentId,
            AcademicSessionId = academicSessionId,
            SchoolClassId = schoolClass.Id,
            SectionId = section?.Id,
            AcademicGroupId = group?.Id,
            ClassName = schoolClass.Name,
            SectionName = section?.Name,
            GroupStream = group?.Name,
            RollNumber = NullIfBlank(rollNumber),
            EffectiveFrom = effectiveFrom.Date,
            IsCurrent = true,
            Status = status,
            Notes = NullIfBlank(notes)
        };

        _db.StudentEnrollments.Add(enrollment);
        student.RollNumber = enrollment.RollNumber;
        student.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, "Student placement saved and previous placement preserved in history.", enrollment);
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
