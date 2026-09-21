using System.Data;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class AdmissionService : IAdmissionService
{
    private readonly ApplicationDbContext _db;

    public AdmissionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Message, int? StudentId, string? AdmissionNumber)> AdmitAsync(
        int applicationId, int schoolId, string? userId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var application = await _db.AdmissionApplications
            .Include(x => x.AdmissionEnquiry)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.SchoolId == schoolId, cancellationToken);

        if (application is null)
            return (false, "Admission application was not found.", null, null);

        if (application.Status == AdmissionApplicationStatus.Admitted && application.StudentId.HasValue)
            return (true, "This application has already been admitted.", application.StudentId, application.AdmissionNumber);

        if (application.Status != AdmissionApplicationStatus.Submitted)
            return (false, "Submit the admission application before approving admission.", null, null);

        var year = application.AdmissionDate.Year;
        var counter = await _db.AdmissionNumberCounters
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.AdmissionYear == year, cancellationToken);

        if (counter is null)
        {
            counter = new AdmissionNumberCounter
            {
                SchoolId = schoolId,
                AdmissionYear = year,
                LastNumber = 0
            };
            _db.AdmissionNumberCounters.Add(counter);
        }

        counter.LastNumber++;
        var admissionNumber = $"STD-{year}-{counter.LastNumber:0000}";

        var guardian = new Guardian
        {
            SchoolId = schoolId,
            FullName = application.GuardianName,
            Relationship = application.GuardianRelationship,
            Phone = application.GuardianPhone,
            Occupation = application.GuardianOccupation,
            Cnic = application.GuardianCnic,
            Address = application.GuardianAddress
        };

        var photo = application.Documents.FirstOrDefault(x => x.DocumentType == AdmissionDocumentType.Photo);
        var student = new Student
        {
            SchoolId = schoolId,
            AdmissionNumber = admissionNumber,
            FullName = application.StudentName,
            FatherGuardianName = application.GuardianName,
            Gender = application.Gender,
            DateOfBirth = application.DateOfBirth,
            BFormCnic = application.BFormCnic,
            Address = application.Address,
            ContactNumber = application.StudentContactNumber,
            PhotoStorageKey = photo?.StorageKey,
            AdmissionDate = application.AdmissionDate,
            Status = StudentStatus.Active
        };

        _db.Students.Add(student);
        _db.Guardians.Add(guardian);
        await _db.SaveChangesAsync(cancellationToken);

        _db.StudentGuardians.Add(new StudentGuardian
        {
            StudentId = student.Id,
            GuardianId = guardian.Id,
            IsPrimary = true
        });

        var matchedClass = await _db.SchoolClasses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.IsActive && x.Name == application.DesiredClass.Trim(), cancellationToken);

        _db.StudentEnrollments.Add(new StudentEnrollment
        {
            SchoolId = schoolId,
            StudentId = student.Id,
            AcademicSessionId = application.AcademicSessionId,
            SchoolClassId = matchedClass?.Id,
            ClassName = matchedClass?.Name ?? application.DesiredClass.Trim(),
            EffectiveFrom = application.AdmissionDate.Date,
            IsCurrent = true,
            Status = StudentEnrollmentStatus.Active,
            Notes = matchedClass is null
                ? "Initial placement created from admission application. Class master match is pending M05 mapping."
                : "Initial placement created from admission application and linked to the M05 class master."
        });

        foreach (var document in application.Documents)
        {
            _db.StudentDocuments.Add(new StudentDocument
            {
                StudentId = student.Id,
                DocumentType = document.DocumentType,
                OriginalFileName = document.OriginalFileName,
                StorageKey = document.StorageKey,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                UploadedAtUtc = document.UploadedAtUtc
            });
        }

        application.StudentId = student.Id;
        application.AdmissionNumber = admissionNumber;
        application.Status = AdmissionApplicationStatus.Admitted;
        application.AdmittedAtUtc = DateTime.UtcNow;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (application.AdmissionEnquiry is not null)
        {
            application.AdmissionEnquiry.Stage = AdmissionEnquiryStage.Admitted;
            application.AdmissionEnquiry.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, $"Student admitted successfully with admission number {admissionNumber}.", student.Id, admissionNumber);
    }
}
