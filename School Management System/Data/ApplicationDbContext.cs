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

    public DbSet<FeeHead> FeeHeads => Set<FeeHead>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<StudentDiscount> StudentDiscounts => Set<StudentDiscount>();
    public DbSet<FeeChallanBatch> FeeChallanBatches => Set<FeeChallanBatch>();
    public DbSet<FeeChallan> FeeChallans => Set<FeeChallan>();
    public DbSet<FeeChallanItem> FeeChallanItems => Set<FeeChallanItem>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<FeePaymentAllocation> FeePaymentAllocations => Set<FeePaymentAllocation>();
    public DbSet<FinancialNumberCounter> FinancialNumberCounters => Set<FinancialNumberCounter>();

    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamClass> ExamClasses => Set<ExamClass>();
    public DbSet<ExamSubject> ExamSubjects => Set<ExamSubject>();
    public DbSet<ExamMarksSheet> ExamMarksSheets => Set<ExamMarksSheet>();
    public DbSet<StudentMark> StudentMarks => Set<StudentMark>();
    public DbSet<StudentResult> StudentResults => Set<StudentResult>();

    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffDocument> StaffDocuments => Set<StaffDocument>();
    public DbSet<StaffNumberCounter> StaffNumberCounters => Set<StaffNumberCounter>();
    public DbSet<StaffAttendance> StaffAttendances => Set<StaffAttendance>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<StaffAdvance> StaffAdvances => Set<StaffAdvance>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
    public DbSet<PayrollAdvanceDeduction> PayrollAdvanceDeductions => Set<PayrollAdvanceDeduction>();
    public DbSet<StaffBiometricEnrollment> StaffBiometricEnrollments => Set<StaffBiometricEnrollment>();
    public DbSet<StaffAttendanceEvent> StaffAttendanceEvents => Set<StaffAttendanceEvent>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<OtherIncome> OtherIncomes => Set<OtherIncome>();
    public DbSet<PromotionBatch> PromotionBatches => Set<PromotionBatch>();
    public DbSet<PromotionItem> PromotionItems => Set<PromotionItem>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<SchoolDocument> SchoolDocuments => Set<SchoolDocument>();
    public DbSet<CommunicationHistory> CommunicationHistory => Set<CommunicationHistory>();
    public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
    public DbSet<RestoreRecord> RestoreRecords => Set<RestoreRecord>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

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

            entity.HasIndex(x => x.StaffId)
                .IsUnique()
                .HasFilter("[StaffId] IS NOT NULL");

            entity.HasOne<Staff>()
                .WithOne()
                .HasForeignKey<ApplicationUser>(x => x.StaffId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Action);
            entity.HasIndex(x => new { x.SchoolId, x.CreatedAtUtc });
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
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.StudentEnrollment)
                .WithMany()
                .HasForeignKey(x => x.StudentEnrollmentId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.StudentAttendance)
                .WithMany()
                .HasForeignKey(x => x.StudentAttendanceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.BiometricDevice)
                .WithMany(x => x.AttendanceEvents)
                .HasForeignKey(x => x.BiometricDeviceId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<FeeHead>(entity =>
        {
            entity.Property(x => x.DefaultAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.Name });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FeeStructure>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.Scope, x.IsActive });
            entity.HasIndex(x => new { x.AcademicSessionId, x.SchoolClassId, x.FeeHeadId });
            entity.HasIndex(x => new { x.AcademicSessionId, x.StudentId, x.FeeHeadId });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FeeHead).WithMany().HasForeignKey(x => x.FeeHeadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentDiscount>(entity =>
        {
            entity.Property(x => x.Value).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.StudentId, x.IsActive });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FeeHead).WithMany().HasForeignKey(x => x.FeeHeadId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FeeChallanBatch>(entity =>
        {
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.BatchKey }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.BillingPeriod });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FeeChallan>(entity =>
        {
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            entity.Property(x => x.LateFeeAmount).HasPrecision(18, 2);
            entity.Property(x => x.CurrentChargesTotal).HasPrecision(18, 2);
            entity.Property(x => x.PreviousOutstandingAtIssue).HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.ChallanNumber }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.StudentId, x.BillingPeriod });
            entity.HasIndex(x => new { x.SchoolId, x.DueDate, x.Status });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StudentEnrollment).WithMany().HasForeignKey(x => x.StudentEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Batch).WithMany(x => x.Challans).HasForeignKey(x => x.FeeChallanBatchId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FeeChallanItem>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.NetAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.FeeChallanId);
            entity.HasOne(x => x.FeeChallan).WithMany(x => x.Items).HasForeignKey(x => x.FeeChallanId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.FeeHead).WithMany().HasForeignKey(x => x.FeeHeadId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FeePayment>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.ReceiptNumber }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.RequestId }).IsUnique().HasFilter("[RequestId] IS NOT NULL");
            entity.HasIndex(x => new { x.SchoolId, x.PaymentDateUtc });
            entity.HasIndex(x => new { x.SchoolId, x.StudentId, x.IsReversed });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FeePaymentAllocation>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.FeePaymentId, x.FeeChallanId, x.FeeChallanItemId }).IsUnique();
            entity.HasOne(x => x.FeePayment).WithMany(x => x.Allocations).HasForeignKey(x => x.FeePaymentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.FeeChallan).WithMany(x => x.PaymentAllocations).HasForeignKey(x => x.FeeChallanId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FeeChallanItem).WithMany(x => x.PaymentAllocations).HasForeignKey(x => x.FeeChallanItemId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FinancialNumberCounter>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.NumberType, x.Year }).IsUnique();
        });

        builder.Entity<Exam>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.StartDate });
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.Title });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ExamClass>(entity =>
        {
            entity.HasIndex(x => new { x.ExamId, x.SchoolClassId }).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Exam).WithMany(x => x.ExamClasses).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExamSubject>(entity =>
        {
            entity.Property(x => x.MaxMarks).HasPrecision(10, 2);
            entity.Property(x => x.PassMarks).HasPrecision(10, 2);
            entity.Property(x => x.TheoryMaxMarks).HasPrecision(10, 2);
            entity.Property(x => x.PracticalMaxMarks).HasPrecision(10, 2);
            entity.Property(x => x.WeightagePercent).HasPrecision(8, 2);
            entity.HasIndex(x => new { x.ExamId, x.SchoolClassId, x.SubjectId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.ExamId, x.IsActive });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Exam).WithMany(x => x.ExamSubjects).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExamMarksSheet>(entity =>
        {
            entity.HasIndex(x => new { x.ExamSubjectId, x.SectionId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.ExamId, x.Status });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExamSubject).WithMany(x => x.MarksSheets).HasForeignKey(x => x.ExamSubjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentMark>(entity =>
        {
            entity.Property(x => x.TheoryMarks).HasPrecision(10, 2);
            entity.Property(x => x.PracticalMarks).HasPrecision(10, 2);
            entity.Property(x => x.ObtainedMarks).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.ExamSubjectId, x.StudentId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.ExamId, x.StudentId });
            entity.HasIndex(x => x.ExamMarksSheetId);
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExamSubject).WithMany(x => x.StudentMarks).HasForeignKey(x => x.ExamSubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExamMarksSheet).WithMany(x => x.StudentMarks).HasForeignKey(x => x.ExamMarksSheetId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StudentEnrollment).WithMany().HasForeignKey(x => x.StudentEnrollmentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentResult>(entity =>
        {
            entity.Property(x => x.ObtainedMarks).HasPrecision(12, 2);
            entity.Property(x => x.MaximumMarks).HasPrecision(12, 2);
            entity.Property(x => x.Percentage).HasPrecision(6, 2);
            entity.Property(x => x.AttendancePercentage).HasPrecision(6, 2);
            entity.HasIndex(x => new { x.ExamId, x.StudentId, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => new { x.ExamId, x.StudentId })
                .IsUnique()
                .HasFilter("[IsCurrent] = 1")
                .HasDatabaseName("UX_StudentResult_Current");
            entity.HasIndex(x => new { x.SchoolId, x.AcademicSessionId, x.ExamId, x.IsCurrent });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcademicSession).WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StudentEnrollment).WithMany().HasForeignKey(x => x.StudentEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PublishedByUser).WithMany().HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Staff>(entity =>
        {
            entity.Property(x => x.JoiningDate).HasColumnType("date");
            entity.Property(x => x.ExitDate).HasColumnType("date");
            entity.HasIndex(x => new { x.SchoolId, x.EmployeeId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.Cnic })
                .IsUnique()
                .HasFilter("[Cnic] IS NOT NULL");
            entity.HasIndex(x => new { x.SchoolId, x.Status, x.Department });
            entity.HasOne(x => x.School)
                .WithMany()
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SalaryStructure)
                .WithMany(x => x.StaffMembers)
                .HasForeignKey(x => x.SalaryStructureId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<StaffDocument>(entity =>
        {
            entity.HasIndex(x => x.StaffId);
            entity.HasOne(x => x.Staff)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.StaffId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StaffNumberCounter>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.Year }).IsUnique();
        });

        builder.Entity<StaffAttendance>(entity =>
        {
            entity.Property(x => x.AttendanceDate).HasColumnType("date");
            entity.HasIndex(x => new { x.SchoolId, x.StaffId, x.AttendanceDate }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.AttendanceDate, x.Status });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Staff).WithMany(x => x.Attendances).HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalaryStructure>(entity =>
        {
            entity.Property(x => x.BasicSalary).HasPrecision(18, 2);
            entity.Property(x => x.HouseAllowance).HasPrecision(18, 2);
            entity.Property(x => x.MedicalAllowance).HasPrecision(18, 2);
            entity.Property(x => x.TransportAllowance).HasPrecision(18, 2);
            entity.Property(x => x.OtherAllowance).HasPrecision(18, 2);
            entity.Property(x => x.FixedDeduction).HasPrecision(18, 2);
            entity.Property(x => x.AbsenceDeductionPerDay).HasPrecision(18, 2);
            entity.Property(x => x.HalfDayDeductionPerDay).HasPrecision(18, 2);
            entity.Property(x => x.LateDeductionPerOccurrence).HasPrecision(18, 2);
            entity.Property(x => x.LeaveDeductionPerDay).HasPrecision(18, 2);
            entity.Property(x => x.EffectiveFrom).HasColumnType("date");
            entity.HasIndex(x => new { x.SchoolId, x.IsActive, x.EffectiveFrom });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StaffAdvance>(entity =>
        {
            entity.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            entity.Property(x => x.OutstandingBalance).HasPrecision(18, 2);
            entity.Property(x => x.MonthlyInstallment).HasPrecision(18, 2);
            entity.Property(x => x.StartDate).HasColumnType("date");
            entity.HasIndex(x => new { x.SchoolId, x.StaffId, x.Status });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Staff).WithMany(x => x.Advances).HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PayrollRun>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.PeriodYear, x.PeriodMonth }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.Status, x.PeriodYear, x.PeriodMonth });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PayrollItem>(entity =>
        {
            entity.Property(x => x.BasicSalary).HasPrecision(18, 2);
            entity.Property(x => x.FixedAllowances).HasPrecision(18, 2);
            entity.Property(x => x.ManualAllowance).HasPrecision(18, 2);
            entity.Property(x => x.GrossPay).HasPrecision(18, 2);
            entity.Property(x => x.AttendanceDeduction).HasPrecision(18, 2);
            entity.Property(x => x.FixedDeduction).HasPrecision(18, 2);
            entity.Property(x => x.AdvanceDeduction).HasPrecision(18, 2);
            entity.Property(x => x.ManualDeduction).HasPrecision(18, 2);
            entity.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            entity.Property(x => x.NetPay).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.PayrollRunId, x.StaffId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.StaffId });
            entity.HasOne(x => x.PayrollRun).WithMany(x => x.Items).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SalaryStructure).WithMany().HasForeignKey(x => x.SalaryStructureId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PayrollAdvanceDeduction>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.PayrollItemId, x.StaffAdvanceId }).IsUnique();
            entity.HasOne(x => x.PayrollItem).WithMany(x => x.AdvanceDeductions).HasForeignKey(x => x.PayrollItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.StaffAdvance).WithMany().HasForeignKey(x => x.StaffAdvanceId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StaffBiometricEnrollment>(entity =>
        {
            entity.HasIndex(x => new { x.BiometricDeviceId, x.DeviceUserReference })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            entity.HasIndex(x => new { x.BiometricDeviceId, x.StaffId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BiometricDevice).WithMany().HasForeignKey(x => x.BiometricDeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StaffAttendanceEvent>(entity =>
        {
            entity.Property(x => x.LocalDate).HasColumnType("date");
            entity.HasIndex(x => new { x.BiometricDeviceId, x.ExternalEventId })
                .IsUnique()
                .HasFilter("[BiometricDeviceId] IS NOT NULL AND [ExternalEventId] IS NOT NULL");
            entity.HasIndex(x => new { x.SchoolId, x.StaffId, x.OccurredAtUtc });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.StaffAttendance).WithMany().HasForeignKey(x => x.StaffAttendanceId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.BiometricDevice).WithMany().HasForeignKey(x => x.BiometricDeviceId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.IsActive, x.SortOrder });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Expense>(entity =>
        {
            entity.Property(x => x.ExpenseDate).HasColumnType("date");
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.ExpenseDate });
            entity.HasIndex(x => new { x.SchoolId, x.ApprovalStatus, x.IsCancelled });
            entity.HasIndex(x => new { x.SchoolId, x.ExpenseCategoryId, x.ExpenseDate });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExpenseCategory).WithMany(x => x.Expenses).HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OtherIncome>(entity =>
        {
            entity.Property(x => x.IncomeDate).HasColumnType("date");
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.SchoolId, x.IncomeDate });
            entity.HasIndex(x => new { x.SchoolId, x.IsCancelled });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });


        builder.Entity<PromotionBatch>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.SourceAcademicSessionId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.TargetAcademicSessionId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.Status });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceAcademicSession).WithMany().HasForeignKey(x => x.SourceAcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetAcademicSession).WithMany().HasForeignKey(x => x.TargetAcademicSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PromotionItem>(entity =>
        {
            entity.Property(x => x.LatestResultPercentage).HasPrecision(6, 2);
            entity.HasIndex(x => new { x.PromotionBatchId, x.StudentId }).IsUnique();
            entity.HasIndex(x => new { x.SchoolId, x.Status });
            entity.HasOne(x => x.PromotionBatch).WithMany(x => x.Items).HasForeignKey(x => x.PromotionBatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceEnrollment).WithMany().HasForeignKey(x => x.SourceEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetEnrollment).WithMany().HasForeignKey(x => x.TargetEnrollmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.SourceSchoolClass).WithMany().HasForeignKey(x => x.SourceSchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceSection).WithMany().HasForeignKey(x => x.SourceSectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceAcademicGroup).WithMany().HasForeignKey(x => x.SourceAcademicGroupId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProposedSchoolClass).WithMany().HasForeignKey(x => x.ProposedSchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProposedSection).WithMany().HasForeignKey(x => x.ProposedSectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProposedAcademicGroup).WithMany().HasForeignKey(x => x.ProposedAcademicGroupId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetSchoolClass).WithMany().HasForeignKey(x => x.TargetSchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetSection).WithMany().HasForeignKey(x => x.TargetSectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetAcademicGroup).WithMany().HasForeignKey(x => x.TargetAcademicGroupId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LatestStudentResult).WithMany().HasForeignKey(x => x.LatestStudentResultId).OnDelete(DeleteBehavior.SetNull);
        });


        builder.Entity<Notice>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.IsPublished, x.IsArchived, x.PublishFromUtc });
            entity.HasIndex(x => new { x.SchoolId, x.Audience, x.SchoolClassId, x.SectionId });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SchoolDocument>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.IsActive, x.Category });
            entity.HasIndex(x => new { x.SchoolId, x.Audience, x.SchoolClassId, x.SectionId });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CommunicationHistory>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.SchoolId, x.Channel, x.Status });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Notice).WithMany(x => x.CommunicationHistory).HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BackupRecord>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.StartedAtUtc });
            entity.HasIndex(x => new { x.SchoolId, x.Status });
            entity.HasIndex(x => x.FileName).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RestoreRecord>(entity =>
        {
            entity.HasIndex(x => new { x.SchoolId, x.StartedAtUtc });
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });



        builder.Entity<SystemSetting>(entity =>
        {
            entity.Property(x => x.LateFeeFixedAmount).HasPrecision(18, 2);
            entity.Property(x => x.LowAttendanceThresholdPercent).HasPrecision(6, 2);
            entity.HasIndex(x => x.SchoolId).IsUnique();
            entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull);
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
