using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Models;

namespace School_Management_System.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<AcademicSession> AcademicSessions => Set<AcademicSession>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<SchoolHoliday> SchoolHolidays => Set<SchoolHoliday>();
    public DbSet<GradingScheme> GradingSchemes => Set<GradingScheme>();
    public DbSet<GradingRule> GradingRules => Set<GradingRule>();

    public DbSet<AdmissionEnquiry> AdmissionEnquiries => Set<AdmissionEnquiry>();
    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();
    public DbSet<AdmissionDocument> AdmissionDocuments => Set<AdmissionDocument>();
    public DbSet<AdmissionNumberCounter> AdmissionNumberCounters => Set<AdmissionNumberCounter>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<AcademicGroup> AcademicGroups => Set<AcademicGroup>();
    public DbSet<StudentAttendance> StudentAttendances => Set<StudentAttendance>();
    public DbSet<BiometricDevice> BiometricDevices => Set<BiometricDevice>();
    public DbSet<BiometricEnrollment> BiometricEnrollments => Set<BiometricEnrollment>();
    public DbSet<CameraFaceEnrollment> CameraFaceEnrollments => Set<CameraFaceEnrollment>();
    public DbSet<AttendanceEvent> AttendanceEvents => Set<AttendanceEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.SchoolId);

            entity.HasOne<School>()
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Action);
        });

        builder.Entity<School>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<AcademicSession>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.SchoolId, x.Name }).IsUnique();

            entity.HasIndex(x => x.SchoolId)
                .IsUnique()
                .HasFilter("[Status] = 1")
                .HasDatabaseName("UX_AcademicSession_OneActivePerSchool");

            entity.HasOne(x => x.School)
                .WithMany(x => x.AcademicSessions)
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Term>(entity =>
        {
            entity.HasIndex(x => new { x.AcademicSessionId, x.Name }).IsUnique();
            entity.HasOne(x => x.AcademicSession)
                .WithMany(x => x.Terms)
                .HasForeignKey(x => x.AcademicSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SchoolHoliday>(entity =>
        {
            entity.HasIndex(x => new { x.AcademicSessionId, x.StartDate, x.Name });
            entity.HasOne(x => x.AcademicSession)
                .WithMany(x => x.Holidays)
                .HasForeignKey(x => x.AcademicSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GradingScheme>(entity =>
        {
            entity.HasIndex(x => new { x.AcademicSessionId, x.Name }).IsUnique();
            entity.HasOne(x => x.AcademicSession)
                .WithMany(x => x.GradingSchemes)
                .HasForeignKey(x => x.AcademicSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GradingRule>(entity =>
        {
            entity.Property(x => x.MinPercentage).HasPrecision(5, 2);
            entity.Property(x => x.MaxPercentage).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.GradingSchemeId, x.Grade }).IsUnique();
            entity.HasOne(x => x.GradingScheme)
                .WithMany(x => x.Rules)
                .HasForeignKey(x => x.GradingSchemeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdmissionEnquiry>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.Stage });
            entity.HasIndex(x => new { x.SchoolId, x.FollowUpDate });
            entity.HasIndex(x => new { x.SchoolId, x.DesiredClass });

            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AcademicSession)
                .WithMany()
                .HasForeignKey(x => x.AcademicSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AdmissionApplication>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.Status });
            entity.HasIndex(x => x.AdmissionEnquiryId)
                .IsUnique()
                .HasFilter("[AdmissionEnquiryId] IS NOT NULL");
            entity.HasIndex(x => x.StudentId)
                .IsUnique()
                .HasFilter("[StudentId] IS NOT NULL");

            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AcademicSession)
                .WithMany()
                .HasForeignKey(x => x.AcademicSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AdmissionEnquiry)
                .WithOne(x => x.AdmissionApplication)
                .HasForeignKey<AdmissionApplication>(x => x.AdmissionEnquiryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AdmissionDocument>(entity =>
        {
            entity.HasIndex(x => x.AdmissionApplicationId);
            entity.HasOne(x => x.AdmissionApplication)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.AdmissionApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdmissionNumberCounter>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.AdmissionYear }).IsUnique();
        });

        builder.Entity<Student>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.AdmissionNumber }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.FullName });
            entity.HasIndex(x => new { x.SchoolId, x.BFormCnic });
            entity.HasIndex(x => new { x.SchoolId, x.RollNumber });

            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Guardian>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.Phone });
        });

        builder.Entity<StudentGuardian>(entity =>
        {
            entity.HasKey(x => new { x.StudentId, x.GuardianId });
            entity.HasOne(x => x.Student)
                .WithMany(x => x.StudentGuardians)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Guardian)
                .WithMany(x => x.StudentGuardians)
                .HasForeignKey(x => x.GuardianId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentDocument>(entity =>
        {
            entity.HasIndex(x => x.StudentId);
            entity.HasOne(x => x.Student)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        builder.Entity<SchoolClass>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Section>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolClassId, x.Name }).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SchoolClass).WithMany(x => x.Sections).HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ClassTeacherUser).WithMany().HasForeignKey(x => x.ClassTeacherUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Subject>(entity =>
        {
            entity.Property(x => x.DefaultMaxMarks).HasPrecision(10, 2);
            entity.Property(x => x.DefaultPassMarks).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.SchoolId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.Title }).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AcademicGroup>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolClassId, x.Name }).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SchoolClass).WithMany(x => x.Groups).HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClassSubject>(entity =>
        {
            entity.Property(x => x.MaxMarks).HasPrecision(10, 2);
            entity.Property(x => x.PassMarks).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.AcademicSessionId, x.SchoolClassId, x.SubjectId }).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SchoolClass).WithMany(x => x.ClassSubjects).HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Subject).WithMany(x => x.ClassSubjects).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeacherAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.TeacherUserId });
            entity.HasIndex(x => new { x.AcademicSessionId, x.SchoolClassId, x.SectionId, x.SubjectId, x.TeacherUserId }).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TeacherUser).WithMany().HasForeignKey(x => x.TeacherUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentAttendance>(entity =>
        {
            entity.Property(x => x.AttendanceDate).HasColumnType("date");
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.StudentId, x.AttendanceDate }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.SchoolClassId, x.SectionId, x.AttendanceDate });
            entity.HasIndex(x => new { x.SchoolId, x.AttendanceDate, x.Status });

            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AcademicSession)
                .WithMany()
                .HasForeignKey(x => x.AcademicSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.StudentEnrollment)
                .WithMany()
                .HasForeignKey(x => x.StudentEnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SchoolClass)
                .WithMany()
                .HasForeignKey(x => x.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Section)
                .WithMany()
                .HasForeignKey(x => x.SectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BiometricDevice>(entity =>
        {
            entity.HasIndex(x => x.DeviceCode).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.IsActive });
            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BiometricEnrollment>(entity =>
        {
            entity.HasIndex(x => new { x.BiometricDeviceId, x.DeviceUserReference }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.StudentId, x.IsActive });
            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BiometricDevice)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.BiometricDeviceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CameraFaceEnrollment>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.StudentId, x.ProviderName }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.IsActive });
            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttendanceEvent>(entity =>
        {
            entity.Property(x => x.LocalDate).HasColumnType("date");
            entity.Property(x => x.ConfidenceScore).HasPrecision(6, 5);
            entity.HasIndex(x => new { x.BiometricDeviceId, x.ExternalEventId })
                .IsUnique()
                .HasFilter("[BiometricDeviceId] IS NOT NULL AND [ExternalEventId] IS NOT NULL");
            entity.HasIndex(x => new { x.SchoolId, x.LocalDate, x.ProcessingStatus });
            entity.HasIndex(x => new { x.SchoolId, x.StudentId, x.OccurredAtUtc });

            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.StudentEnrollment)
                .WithMany()
                .HasForeignKey(x => x.StudentEnrollmentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.StudentAttendance)
                .WithMany()
                .HasForeignKey(x => x.StudentAttendanceId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.BiometricDevice)
                .WithMany(x => x.AttendanceEvents)
                .HasForeignKey(x => x.BiometricDeviceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<StudentEnrollment>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.ClassName, x.SectionName });
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.SchoolClassId, x.SectionId });
            entity.HasIndex(x => new { x.StudentId, x.AcademicSessionId });
            entity.HasIndex(x => x.StudentId)
                .IsUnique()
                .HasFilter("[IsCurrent] = 1")
                .HasDatabaseName("UX_StudentEnrollment_OneCurrentPerStudent");

            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Student)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.AcademicSession)
                .WithMany()
                .HasForeignKey(x => x.AcademicSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SchoolClass)
                .WithMany()
                .HasForeignKey(x => x.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Section)
                .WithMany()
                .HasForeignKey(x => x.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AcademicGroup)
                .WithMany()
                .HasForeignKey(x => x.AcademicGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
