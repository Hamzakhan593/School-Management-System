using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Models;

namespace School_Management_System.Data;

/// <summary>
/// Development-only demo dataset for presenting the School Management System.
/// All people, phone numbers, CNIC/B-Form values and financial figures created here are synthetic demo data.
/// The seeder is guarded by DemoData:Enabled and a database marker so it does not duplicate data on each run.
/// </summary>
public static class DemoDataSeeder
{
    private const string DemoMarker = "DEMO_DATA_SEEDED_V1";
    private const string DemoPassword = "School@12345";
    private const string DemoSchoolName = "The School of Thoughts";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("DemoData:Enabled"))
        {
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");

        try
        {
            if (!await db.Database.CanConnectAsync())
            {
                logger.LogWarning("Demo data was not seeded because the database is not available.");
                return;
            }

            var existingSchool = await db.Schools.FirstOrDefaultAsync(x => x.Name == DemoSchoolName);
            if (existingSchool is not null &&
                await db.AuditLogs.AnyAsync(x => x.SchoolId == existingSchool.Id && x.Action == DemoMarker))
            {
                await EnsurePrincipalLinkedAsync(existingSchool, userManager, configuration);
                logger.LogInformation("Demo data already exists for {SchoolName}; skipping duplicate seed.", DemoSchoolName);
                return;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var today = DateTime.Today;
                var currentSchoolYearStart = today.Month >= 4 ? today.Year : today.Year - 1;
                var school = existingSchool ?? new School
                {
                    Name = DemoSchoolName,
                    RegistrationNumber = "DEMO-SGR-001",
                    Address = "Sanghar, Sindh, Pakistan",
                    Phone = "0300-0000000",
                    Email = "demo@schoolofthoughts.local",
                    PrincipalName = "School Principal",
                    ChallanFooterText = "Demo fee challan — The School of Thoughts, Sanghar",
                    ReceiptFooterText = "Thank you. This receipt is generated from demo data.",
                    IsActive = true
                };

                if (existingSchool is null)
                {
                    db.Schools.Add(school);
                    await db.SaveChangesAsync();
                }
                else
                {
                    school.Address = "Sanghar, Sindh, Pakistan";
                    school.PrincipalName ??= "School Principal";
                    school.IsActive = true;
                    school.UpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                var principal = await EnsurePrincipalLinkedAsync(school, userManager, configuration);
                var principalId = principal?.Id;

                await EnsureSystemSettingsAsync(db, school.Id);

                var previousSession = await EnsureSessionAsync(
                    db,
                    school.Id,
                    $"{currentSchoolYearStart - 1}-{currentSchoolYearStart % 100:00}",
                    new DateTime(currentSchoolYearStart - 1, 4, 1),
                    new DateTime(currentSchoolYearStart, 3, 31),
                    AcademicSessionStatus.Archived,
                    "Previous academic session — demo history");

                var activeSession = await EnsureActiveSessionAsync(
                    db,
                    school.Id,
                    $"{currentSchoolYearStart}-{(currentSchoolYearStart + 1) % 100:00}",
                    new DateTime(currentSchoolYearStart, 4, 1),
                    new DateTime(currentSchoolYearStart + 1, 3, 31));

                await SeedAcademicSetupAsync(db, activeSession, previousSession, today);

                var classes = await SeedClassesAsync(db, school.Id);
                var sections = await SeedSectionsAsync(db, school.Id, classes);
                var groups = await SeedAcademicGroupsAsync(db, school.Id, classes);
                var subjects = await SeedSubjectsAsync(db, school.Id);
                await SeedClassSubjectsAsync(db, school.Id, activeSession.Id, classes, subjects);

                var salaryStructures = await SeedSalaryStructuresAsync(db, school.Id, activeSession.StartDate);
                var staffContext = await SeedStaffAndUsersAsync(
                    db,
                    userManager,
                    school.Id,
                    principal,
                    salaryStructures,
                    currentSchoolYearStart);

                await AssignClassTeachersAndSubjectsAsync(
                    db,
                    school.Id,
                    activeSession.Id,
                    classes,
                    sections,
                    subjects,
                    staffContext.TeacherUsers);

                var students = await SeedStudentsAsync(
                    db,
                    school.Id,
                    activeSession,
                    previousSession,
                    classes,
                    sections,
                    groups,
                    currentSchoolYearStart);

                await SeedAdmissionPipelineAsync(db, school.Id, activeSession.Id, today, principalId);
                await SeedStudentAttendanceAsync(db, school.Id, activeSession.Id, students, today, principalId);
                await SeedFeesAsync(db, school.Id, activeSession.Id, students, classes, today, principalId);
                await SeedExamsAndResultsAsync(db, school.Id, activeSession, students, classes, sections, subjects, today, principalId);
                await SeedStaffAttendanceAndPayrollAsync(db, school.Id, staffContext.StaffMembers, today, principalId);
                await SeedExpensesAndIncomeAsync(db, school.Id, today, principalId);
                await SeedNoticesAsync(db, school.Id, classes, sections, today, principalId);
                await SeedAuditTrailAsync(db, school.Id, today, principal?.Email);

                db.AuditLogs.Add(new AuditLog
                {
                    SchoolId = school.Id,
                    UserId = principalId,
                    UserEmail = principal?.Email ?? "system",
                    Action = DemoMarker,
                    EntityType = "DemoData",
                    EntityId = "V1",
                    Details = "Pakistan/Sindh-context demo dataset created for development and presentation.",
                    CreatedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                logger.LogInformation(
                    "Demo data seeded successfully for {SchoolName}: {StudentCount} students and {StaffCount} staff.",
                    DemoSchoolName,
                    students.Count,
                    staffContext.StaffMembers.Count);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo data seeding failed. No demo marker was written.");
        }
    }

    private static async Task<ApplicationUser?> EnsurePrincipalLinkedAsync(
        School school,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"] ?? "principal@school.local";
        var principal = await userManager.FindByEmailAsync(email);
        if (principal is null)
        {
            return null;
        }

        var changed = false;
        if (principal.SchoolId != school.Id)
        {
            principal.SchoolId = school.Id;
            changed = true;
        }
        if (!principal.IsActive)
        {
            principal.IsActive = true;
            changed = true;
        }
        if (changed)
        {
            await userManager.UpdateAsync(principal);
        }

        if (!await userManager.IsInRoleAsync(principal, AppRoles.Principal))
        {
            await userManager.AddToRoleAsync(principal, AppRoles.Principal);
        }

        return principal;
    }

    private static async Task EnsureSystemSettingsAsync(ApplicationDbContext db, int schoolId)
    {
        var settings = await db.SystemSettings.FirstOrDefaultAsync(x => x.SchoolId == schoolId);
        if (settings is null)
        {
            settings = new SystemSetting
            {
                SchoolId = schoolId,
                AdmissionNumberPrefix = "STD",
                AdmissionNumberDigits = 4,
                ChallanNumberPrefix = "CH",
                ReceiptNumberPrefix = "RC",
                FinancialNumberDigits = 6,
                FiscalYearStartMonth = 4,
                DefaultFeeDueDay = 10,
                LateFeeFixedAmount = 200m,
                LateFeeGraceDays = 5,
                ApplyLateFeeOnCollection = true,
                AutoGenerateMonthlyChallans = false,
                MonthlyChallanGenerationDay = 1,
                TeacherAttendanceEditCutoffHours = 24,
                LowAttendanceThresholdPercent = 75m,
                ResultCardShowAttendance = true,
                ResultCardShowClassPosition = true,
                ScheduledBackupsEnabled = true,
                BackupHourLocal = 2,
                BackupRetentionDays = 30,
                SessionTimeoutMinutes = 30,
                PasswordRequiredLength = 8,
                PasswordRequireDigit = true,
                PasswordRequireUppercase = true,
                PasswordRequireLowercase = true,
                SchoolTimeZoneId = "Asia/Karachi"
            };
            db.SystemSettings.Add(settings);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<AcademicSession> EnsureSessionAsync(
        ApplicationDbContext db,
        int schoolId,
        string name,
        DateTime startDate,
        DateTime endDate,
        AcademicSessionStatus status,
        string notes)
    {
        var session = await db.AcademicSessions.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Name == name);
        if (session is null)
        {
            session = new AcademicSession
            {
                SchoolId = schoolId,
                Name = name,
                StartDate = startDate,
                EndDate = endDate,
                WorkingDaysPerWeek = 6,
                Status = status,
                Notes = notes
            };
            db.AcademicSessions.Add(session);
            await db.SaveChangesAsync();
        }
        return session;
    }

    private static async Task<AcademicSession> EnsureActiveSessionAsync(
        ApplicationDbContext db,
        int schoolId,
        string name,
        DateTime startDate,
        DateTime endDate)
    {
        var session = await db.AcademicSessions.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Name == name);
        if (session is null)
        {
            var otherActive = await db.AcademicSessions.Where(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active).ToListAsync();
            foreach (var item in otherActive)
            {
                item.Status = AcademicSessionStatus.Archived;
                item.UpdatedAtUtc = DateTime.UtcNow;
            }

            session = new AcademicSession
            {
                SchoolId = schoolId,
                Name = name,
                StartDate = startDate,
                EndDate = endDate,
                WorkingDaysPerWeek = 6,
                Status = AcademicSessionStatus.Active,
                Notes = "Current academic session — demo dataset"
            };
            db.AcademicSessions.Add(session);
            await db.SaveChangesAsync();
        }
        else if (session.Status != AcademicSessionStatus.Active)
        {
            var otherActive = await db.AcademicSessions.Where(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active && x.Id != session.Id).ToListAsync();
            foreach (var item in otherActive)
            {
                item.Status = AcademicSessionStatus.Archived;
            }
            session.Status = AcademicSessionStatus.Active;
            session.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return session;
    }

    private static async Task SeedAcademicSetupAsync(
        ApplicationDbContext db,
        AcademicSession activeSession,
        AcademicSession previousSession,
        DateTime today)
    {
        if (!await db.Terms.AnyAsync(x => x.AcademicSessionId == activeSession.Id))
        {
            db.Terms.AddRange(
                new Term
                {
                    AcademicSessionId = activeSession.Id,
                    Name = "First Term",
                    StartDate = activeSession.StartDate,
                    EndDate = new DateTime(activeSession.StartDate.Year, 9, 30),
                    DisplayOrder = 1
                },
                new Term
                {
                    AcademicSessionId = activeSession.Id,
                    Name = "Second Term",
                    StartDate = new DateTime(activeSession.StartDate.Year, 10, 1),
                    EndDate = activeSession.EndDate,
                    DisplayOrder = 2
                });
        }

        if (!await db.SchoolHolidays.AnyAsync(x => x.AcademicSessionId == activeSession.Id))
        {
            db.SchoolHolidays.AddRange(
                new SchoolHoliday
                {
                    AcademicSessionId = activeSession.Id,
                    Name = "Pakistan Day",
                    StartDate = new DateTime(activeSession.EndDate.Year, 3, 23),
                    EndDate = new DateTime(activeSession.EndDate.Year, 3, 23),
                    Notes = "Public holiday — demo calendar"
                },
                new SchoolHoliday
                {
                    AcademicSessionId = activeSession.Id,
                    Name = "Independence Day",
                    StartDate = new DateTime(activeSession.StartDate.Year, 8, 14),
                    EndDate = new DateTime(activeSession.StartDate.Year, 8, 14),
                    Notes = "Public holiday — demo calendar"
                },
                new SchoolHoliday
                {
                    AcademicSessionId = activeSession.Id,
                    Name = "Winter Break",
                    StartDate = new DateTime(activeSession.StartDate.Year, 12, 22),
                    EndDate = new DateTime(activeSession.StartDate.Year + 1, 1, 2),
                    Notes = "Demo winter vacation"
                });
        }

        if (!await db.GradingSchemes.AnyAsync(x => x.AcademicSessionId == activeSession.Id))
        {
            var scheme = new GradingScheme
            {
                AcademicSessionId = activeSession.Id,
                Name = "School Grading Scheme",
                IsDefault = true
            };
            scheme.Rules.Add(new GradingRule { Grade = "A+", MinPercentage = 90m, MaxPercentage = 100m, Remarks = "Outstanding" });
            scheme.Rules.Add(new GradingRule { Grade = "A", MinPercentage = 80m, MaxPercentage = 89.99m, Remarks = "Excellent" });
            scheme.Rules.Add(new GradingRule { Grade = "B", MinPercentage = 70m, MaxPercentage = 79.99m, Remarks = "Very Good" });
            scheme.Rules.Add(new GradingRule { Grade = "C", MinPercentage = 60m, MaxPercentage = 69.99m, Remarks = "Good" });
            scheme.Rules.Add(new GradingRule { Grade = "D", MinPercentage = 50m, MaxPercentage = 59.99m, Remarks = "Satisfactory" });
            scheme.Rules.Add(new GradingRule { Grade = "E", MinPercentage = 40m, MaxPercentage = 49.99m, Remarks = "Needs Improvement" });
            scheme.Rules.Add(new GradingRule { Grade = "F", MinPercentage = 0m, MaxPercentage = 39.99m, Remarks = "Fail" });
            db.GradingSchemes.Add(scheme);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, SchoolClass>> SeedClassesAsync(ApplicationDbContext db, int schoolId)
    {
        var definitions = new (string Name, string Code, int Order)[]
        {
            ("Nursery", "NUR", 1),
            ("KG", "KG", 2),
            ("Class 1", "C01", 3),
            ("Class 2", "C02", 4),
            ("Class 3", "C03", 5),
            ("Class 4", "C04", 6),
            ("Class 5", "C05", 7),
            ("Class 6", "C06", 8),
            ("Class 7", "C07", 9),
            ("Class 8", "C08", 10),
            ("Class 9", "C09", 11),
            ("Class 10", "C10", 12)
        };

        var existing = await db.SchoolClasses.Where(x => x.SchoolId == schoolId).ToListAsync();
        foreach (var item in definitions)
        {
            if (existing.All(x => x.Name != item.Name))
            {
                db.SchoolClasses.Add(new SchoolClass
                {
                    SchoolId = schoolId,
                    Name = item.Name,
                    Code = item.Code,
                    SortOrder = item.Order,
                    IsActive = true
                });
            }
        }
        await db.SaveChangesAsync();
        return await db.SchoolClasses.Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Name);
    }

    private static async Task<Dictionary<string, Section>> SeedSectionsAsync(
        ApplicationDbContext db,
        int schoolId,
        Dictionary<string, SchoolClass> classes)
    {
        foreach (var schoolClass in classes.Values.OrderBy(x => x.SortOrder))
        {
            foreach (var sectionName in new[] { "A", "B" })
            {
                if (!await db.Sections.AnyAsync(x => x.SchoolClassId == schoolClass.Id && x.Name == sectionName))
                {
                    db.Sections.Add(new Section
                    {
                        SchoolId = schoolId,
                        SchoolClassId = schoolClass.Id,
                        Name = sectionName,
                        Capacity = 35,
                        Classroom = $"{schoolClass.Code}-{sectionName}",
                        IsActive = true
                    });
                }
            }
        }
        await db.SaveChangesAsync();

        return await db.Sections
            .Where(x => x.SchoolId == schoolId)
            .Include(x => x.SchoolClass)
            .ToDictionaryAsync(x => $"{x.SchoolClass.Name}|{x.Name}");
    }

    private static async Task<Dictionary<string, AcademicGroup>> SeedAcademicGroupsAsync(
        ApplicationDbContext db,
        int schoolId,
        Dictionary<string, SchoolClass> classes)
    {
        foreach (var className in new[] { "Class 9", "Class 10" })
        {
            var schoolClass = classes[className];
            foreach (var groupName in new[] { "Science", "Computer Science" })
            {
                if (!await db.AcademicGroups.AnyAsync(x => x.SchoolClassId == schoolClass.Id && x.Name == groupName))
                {
                    db.AcademicGroups.Add(new AcademicGroup
                    {
                        SchoolId = schoolId,
                        SchoolClassId = schoolClass.Id,
                        Name = groupName,
                        Description = $"{groupName} group — demo data",
                        IsActive = true
                    });
                }
            }
        }
        await db.SaveChangesAsync();

        return await db.AcademicGroups
            .Where(x => x.SchoolId == schoolId)
            .Include(x => x.SchoolClass)
            .ToDictionaryAsync(x => $"{x.SchoolClass.Name}|{x.Name}");
    }

    private static async Task<Dictionary<string, Subject>> SeedSubjectsAsync(ApplicationDbContext db, int schoolId)
    {
        var definitions = new (string Code, string Title, bool Practical)[]
        {
            ("ENG", "English", false),
            ("URD", "Urdu", false),
            ("SND", "Sindhi", false),
            ("MATH", "Mathematics", false),
            ("GSCI", "General Science", false),
            ("GK", "General Knowledge", false),
            ("ISL", "Islamiat", false),
            ("SST", "Social Studies", false),
            ("PAK", "Pakistan Studies", false),
            ("COMP", "Computer Science", true),
            ("PHY", "Physics", true),
            ("CHEM", "Chemistry", true),
            ("BIO", "Biology", true)
        };

        var existing = await db.Subjects.Where(x => x.SchoolId == schoolId).ToListAsync();
        foreach (var item in definitions)
        {
            if (existing.All(x => x.Code != item.Code))
            {
                db.Subjects.Add(new Subject
                {
                    SchoolId = schoolId,
                    Code = item.Code,
                    Title = item.Title,
                    HasTheory = true,
                    HasPractical = item.Practical,
                    DefaultMaxMarks = 100m,
                    DefaultPassMarks = 40m,
                    IsActive = true
                });
            }
        }
        await db.SaveChangesAsync();
        return await db.Subjects.Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Code);
    }

    private static async Task SeedClassSubjectsAsync(
        ApplicationDbContext db,
        int schoolId,
        int sessionId,
        Dictionary<string, SchoolClass> classes,
        Dictionary<string, Subject> subjects)
    {
        foreach (var schoolClass in classes.Values.OrderBy(x => x.SortOrder))
        {
            var codes = SubjectCodesForClass(schoolClass.Name);
            foreach (var code in codes)
            {
                var subject = subjects[code];
                if (!await db.ClassSubjects.AnyAsync(x => x.AcademicSessionId == sessionId && x.SchoolClassId == schoolClass.Id && x.SubjectId == subject.Id))
                {
                    db.ClassSubjects.Add(new ClassSubject
                    {
                        SchoolId = schoolId,
                        AcademicSessionId = sessionId,
                        SchoolClassId = schoolClass.Id,
                        SubjectId = subject.Id,
                        MaxMarks = 100m,
                        PassMarks = 40m,
                        IsActive = true
                    });
                }
            }
        }
        await db.SaveChangesAsync();
    }

    private static IReadOnlyList<string> SubjectCodesForClass(string className)
    {
        if (className is "Nursery" or "KG")
            return new[] { "ENG", "URD", "SND", "MATH", "GK", "ISL" };

        if (className is "Class 1" or "Class 2" or "Class 3" or "Class 4" or "Class 5")
            return new[] { "ENG", "URD", "SND", "MATH", "GSCI", "ISL", "SST", "COMP" };

        if (className is "Class 6" or "Class 7" or "Class 8")
            return new[] { "ENG", "URD", "SND", "MATH", "GSCI", "ISL", "PAK", "COMP" };

        return new[] { "ENG", "URD", "SND", "MATH", "ISL", "PAK", "PHY", "CHEM", "BIO", "COMP" };
    }

    private static async Task<Dictionary<string, SalaryStructure>> SeedSalaryStructuresAsync(
        ApplicationDbContext db,
        int schoolId,
        DateTime effectiveFrom)
    {
        var definitions = new[]
        {
            new SalarySeed("Principal Package", 85000m, 10000m, 5000m, 5000m, 5000m),
            new SalarySeed("Administration Package", 55000m, 6000m, 3500m, 3000m, 2500m),
            new SalarySeed("Senior Teacher Package", 45000m, 5000m, 3000m, 2500m, 1500m),
            new SalarySeed("Teacher Package", 38000m, 4000m, 2500m, 2000m, 1000m),
            new SalarySeed("Office Support Package", 30000m, 2500m, 1500m, 1500m, 500m),
            new SalarySeed("Support Staff Package", 24000m, 1500m, 1000m, 1000m, 500m)
        };

        foreach (var item in definitions)
        {
            if (!await db.SalaryStructures.AnyAsync(x => x.SchoolId == schoolId && x.Name == item.Name))
            {
                db.SalaryStructures.Add(new SalaryStructure
                {
                    SchoolId = schoolId,
                    Name = item.Name,
                    BasicSalary = item.Basic,
                    HouseAllowance = item.House,
                    MedicalAllowance = item.Medical,
                    TransportAllowance = item.Transport,
                    OtherAllowance = item.Other,
                    FixedDeduction = 0m,
                    AbsenceDeductionPerDay = Math.Round(item.Basic / 26m, 2),
                    HalfDayDeductionPerDay = Math.Round(item.Basic / 52m, 2),
                    LateDeductionPerOccurrence = 200m,
                    LeaveDeductionPerDay = 0m,
                    EffectiveFrom = effectiveFrom,
                    IsActive = true
                });
            }
        }
        await db.SaveChangesAsync();
        return await db.SalaryStructures.Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Name);
    }

    private static async Task<StaffSeedContext> SeedStaffAndUsersAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        int schoolId,
        ApplicationUser? principal,
        Dictionary<string, SalaryStructure> salaryStructures,
        int currentSchoolYearStart)
    {
        var seeds = new[]
        {
            new StaffSeed("EMP-2026-0001", "School Principal", "Principal", "Administration", "M.Ed. / School Leadership", "Principal Package", AppRoles.Principal, principal?.Email),
            new StaffSeed("EMP-2026-0002", "Naveed Ahmed", "School Administrator", "Administration", "MBA / B.Ed.", "Administration Package", AppRoles.Admin, "admin.demo@school.local"),
            new StaffSeed("EMP-2026-0003", "Sana Memon", "Accountant", "Accounts", "B.Com / M.Com", "Administration Package", AppRoles.Accountant, "accounts.demo@school.local"),
            new StaffSeed("EMP-2026-0004", "Farhan Ali", "HR Officer", "Human Resources", "BBA", "Administration Package", AppRoles.HR, "hr.demo@school.local"),
            new StaffSeed("EMP-2026-0005", "Ayesha Shaikh", "Receptionist", "Front Office", "B.A.", "Office Support Package", AppRoles.Receptionist, "reception.demo@school.local"),
            new StaffSeed("EMP-2026-0006", "Imran Raza", "Exam Controller", "Examinations", "M.Sc. / B.Ed.", "Senior Teacher Package", AppRoles.ExamController, "exam.demo@school.local"),
            new StaffSeed("EMP-2026-0007", "Hina Khaskheli", "Senior Teacher", "English", "M.A. English / B.Ed.", "Senior Teacher Package", AppRoles.Teacher, "teacher1.demo@school.local"),
            new StaffSeed("EMP-2026-0008", "Asad Hussain", "Teacher", "Mathematics", "M.Sc. Mathematics / B.Ed.", "Senior Teacher Package", AppRoles.Teacher, "teacher2.demo@school.local"),
            new StaffSeed("EMP-2026-0009", "Nadia Baloch", "Teacher", "Science", "M.Sc. Chemistry / B.Ed.", "Teacher Package", AppRoles.Teacher, "teacher3.demo@school.local"),
            new StaffSeed("EMP-2026-0010", "Kamran Memon", "Teacher", "Science", "M.Sc. Physics / B.Ed.", "Teacher Package", AppRoles.Teacher, "teacher4.demo@school.local"),
            new StaffSeed("EMP-2026-0011", "Mehwish Rajput", "Teacher", "Biology", "M.Sc. Zoology / B.Ed.", "Teacher Package", AppRoles.Teacher, "teacher5.demo@school.local"),
            new StaffSeed("EMP-2026-0012", "Bilal Ahmed", "Teacher", "Computer Science", "BS Computer Science", "Teacher Package", AppRoles.Teacher, "teacher6.demo@school.local"),
            new StaffSeed("EMP-2026-0013", "Saira Qureshi", "Teacher", "Urdu / Sindhi", "M.A. Urdu / B.Ed.", "Teacher Package", AppRoles.Teacher, "teacher7.demo@school.local"),
            new StaffSeed("EMP-2026-0014", "Abdul Rehman", "Teacher", "Pakistan Studies", "M.A. Pakistan Studies / B.Ed.", "Teacher Package", AppRoles.Teacher, "teacher8.demo@school.local"),
            new StaffSeed("EMP-2026-0015", "Maryam Siddiqui", "Junior Teacher", "Primary", "B.A. / B.Ed.", "Teacher Package", AppRoles.Teacher, "teacher9.demo@school.local"),
            new StaffSeed("EMP-2026-0016", "Waqas Ali", "Junior Teacher", "Primary", "B.Sc. / B.Ed.", "Teacher Package", AppRoles.Teacher, "teacher10.demo@school.local"),
            new StaffSeed("EMP-2026-0017", "Zubair Khan", "IT Assistant", "IT", "BS Information Technology", "Office Support Package", null, null),
            new StaffSeed("EMP-2026-0018", "Rabia Memon", "Office Assistant", "Administration", "B.A.", "Office Support Package", null, null),
            new StaffSeed("EMP-2026-0019", "Shahid Ali", "Security Guard", "Support", "Matric", "Support Staff Package", null, null),
            new StaffSeed("EMP-2026-0020", "Rukhsana Bibi", "School Attendant", "Support", "Middle", "Support Staff Package", null, null),
            new StaffSeed("EMP-2026-0021", "Ghulam Mustafa", "Peon", "Support", "Matric", "Support Staff Package", null, null),
            new StaffSeed("EMP-2026-0022", "Nasreen Bibi", "Cleaner", "Support", "Primary", "Support Staff Package", null, null)
        };

        var staffList = new List<Staff>();
        var teacherUsers = new List<ApplicationUser>();

        for (var i = 0; i < seeds.Length; i++)
        {
            var seed = seeds[i];
            var staff = await db.Staff.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.EmployeeId == seed.EmployeeId);
            if (staff is null)
            {
                staff = new Staff
                {
                    SchoolId = schoolId,
                    EmployeeId = seed.EmployeeId,
                    FullName = seed.FullName,
                    Cnic = $"45303-{7000000 + i:0000000}-{(i % 9) + 1}",
                    Phone = $"03{10 + (i % 8):00}-{2000000 + i:0000000}",
                    Email = seed.Email,
                    Address = $"Demo residential area, Sanghar, Sindh ({i + 1})",
                    Designation = seed.Designation,
                    Department = seed.Department,
                    Qualification = seed.Qualification,
                    JoiningDate = new DateTime(currentSchoolYearStart - Math.Min(5, i % 6), 3, 1).AddDays(i),
                    EmploymentType = i >= 18 ? StaffEmploymentType.Contract : StaffEmploymentType.Permanent,
                    Status = StaffStatus.Active,
                    SalaryStructureId = salaryStructures[seed.SalaryPackage].Id,
                    Notes = "Synthetic demo staff record"
                };
                db.Staff.Add(staff);
                await db.SaveChangesAsync();
            }
            staffList.Add(staff);

            if (!string.IsNullOrWhiteSpace(seed.Email) && !string.IsNullOrWhiteSpace(seed.Role))
            {
                var user = await userManager.FindByEmailAsync(seed.Email);
                if (user is null)
                {
                    user = new ApplicationUser
                    {
                        UserName = seed.Email,
                        Email = seed.Email,
                        EmailConfirmed = true,
                        FullName = seed.FullName,
                        SchoolId = schoolId,
                        StaffId = staff.Id,
                        IsActive = true
                    };
                    var create = await userManager.CreateAsync(user, DemoPassword);
                    if (!create.Succeeded)
                    {
                        throw new InvalidOperationException($"Could not create demo user {seed.Email}: {string.Join("; ", create.Errors.Select(x => x.Description))}");
                    }
                }
                else
                {
                    user.FullName = seed.FullName;
                    user.SchoolId = schoolId;
                    user.StaffId ??= staff.Id;
                    user.IsActive = true;
                    await userManager.UpdateAsync(user);
                }

                if (!await userManager.IsInRoleAsync(user, seed.Role))
                {
                    await userManager.AddToRoleAsync(user, seed.Role);
                }

                if (seed.Role == AppRoles.Teacher)
                {
                    teacherUsers.Add(user);
                }
            }
        }

        var yearCounter = await db.StaffNumberCounters.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Year == 2026);
        if (yearCounter is null)
        {
            db.StaffNumberCounters.Add(new StaffNumberCounter { SchoolId = schoolId, Year = 2026, LastNumber = 22 });
        }
        else if (yearCounter.LastNumber < 22)
        {
            yearCounter.LastNumber = 22;
        }
        await db.SaveChangesAsync();

        return new StaffSeedContext(staffList, teacherUsers);
    }

    private static async Task AssignClassTeachersAndSubjectsAsync(
        ApplicationDbContext db,
        int schoolId,
        int sessionId,
        Dictionary<string, SchoolClass> classes,
        Dictionary<string, Section> sections,
        Dictionary<string, Subject> subjects,
        List<ApplicationUser> teacherUsers)
    {
        if (teacherUsers.Count == 0)
            return;

        var orderedClasses = classes.Values.OrderBy(x => x.SortOrder).ToList();
        for (var i = 0; i < orderedClasses.Count; i++)
        {
            var schoolClass = orderedClasses[i];
            foreach (var sectionName in new[] { "A", "B" })
            {
                var section = sections[$"{schoolClass.Name}|{sectionName}"];
                var teacher = teacherUsers[(i * 2 + (sectionName == "B" ? 1 : 0)) % teacherUsers.Count];
                section.ClassTeacherUserId = teacher.Id;

                foreach (var code in SubjectCodesForClass(schoolClass.Name).Take(4))
                {
                    var subject = subjects[code];
                    if (!await db.TeacherAssignments.AnyAsync(x =>
                            x.AcademicSessionId == sessionId &&
                            x.SchoolClassId == schoolClass.Id &&
                            x.SectionId == section.Id &&
                            x.SubjectId == subject.Id &&
                            x.TeacherUserId == teacher.Id))
                    {
                        db.TeacherAssignments.Add(new TeacherAssignment
                        {
                            SchoolId = schoolId,
                            AcademicSessionId = sessionId,
                            SchoolClassId = schoolClass.Id,
                            SectionId = section.Id,
                            SubjectId = subject.Id,
                            TeacherUserId = teacher.Id,
                            Notes = "Demo teaching assignment",
                            IsActive = true
                        });
                    }
                }
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task<List<StudentSeedRow>> SeedStudentsAsync(
        ApplicationDbContext db,
        int schoolId,
        AcademicSession activeSession,
        AcademicSession previousSession,
        Dictionary<string, SchoolClass> classes,
        Dictionary<string, Section> sections,
        Dictionary<string, AcademicGroup> groups,
        int currentSchoolYearStart)
    {
        var firstNamesMale = new[] { "Muhammad Ali", "Ahmed Raza", "Hamza", "Bilal", "Abdul Rehman", "Hassan", "Huzaifa", "Saad", "Ayan", "Danish", "Taha", "Usman", "Zain", "Ibrahim", "Shayan" };
        var firstNamesFemale = new[] { "Ayesha", "Fatima", "Zainab", "Hira", "Mahnoor", "Maryam", "Sana", "Anaya", "Eman", "Laiba", "Dua", "Areeba", "Noor", "Hafsa", "Mehak" };
        var surnames = new[] { "Memon", "Khaskheli", "Shaikh", "Rajput", "Qureshi", "Baloch", "Siddiqui", "Khan", "Abbasi", "Junejo", "Pathan", "Mahar" };
        var occupations = new[] { "Shopkeeper", "Teacher", "Farmer", "Government Employee", "Private Employee", "Business", "Driver", "Technician", "Accountant", "Tailor" };
        var areas = new[]
        {
            "Jinnah Road area, Sanghar",
            "Shahdadpur Road area, Sanghar",
            "Nawabshah Road area, Sanghar",
            "Housing Society area, Sanghar",
            "Civil Hospital Road area, Sanghar",
            "Main Bazaar area, Sanghar",
            "Demo Colony, Sanghar",
            "Railway Station area, Sanghar"
        };

        var orderedClasses = classes.Values.OrderBy(x => x.SortOrder).ToList();
        var rows = new List<StudentSeedRow>();
        var random = new Random(20260925);
        const int totalStudents = 120;

        for (var i = 1; i <= totalStudents; i++)
        {
            var classIndex = (i - 1) % orderedClasses.Count;
            var schoolClass = orderedClasses[classIndex];
            var classCycle = (i - 1) / orderedClasses.Count;
            var sectionName = classCycle % 2 == 0 ? "A" : "B";
            var section = sections[$"{schoolClass.Name}|{sectionName}"];
            var gender = i % 2 == 0 ? "Female" : "Male";
            var first = gender == "Male"
                ? firstNamesMale[(i - 1) % firstNamesMale.Length]
                : firstNamesFemale[(i - 1) % firstNamesFemale.Length];
            var surname = surnames[(i * 3) % surnames.Length];
            var fullName = $"{first} {surname}";
            var fatherName = $"{firstNamesMale[(i * 5) % firstNamesMale.Length]} {surnames[(i * 7) % surnames.Length]}";
            var age = 4 + classIndex;
            var dob = todayForBirth(activeSession.StartDate, age, i);
            var admissionYear = currentSchoolYearStart - Math.Min(3, classIndex / 3);
            var admissionDate = new DateTime(admissionYear, 4, 5).AddDays(i % 40);
            var admissionNo = $"STD-{admissionYear}-{i:0000}";
            var roll = $"{sectionName}-{(classCycle / 2) + 1:00}";

            var student = await db.Students.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.AdmissionNumber == admissionNo);
            if (student is null)
            {
                student = new Student
                {
                    SchoolId = schoolId,
                    AdmissionNumber = admissionNo,
                    RollNumber = roll,
                    FullName = fullName,
                    FatherGuardianName = fatherName,
                    Gender = gender,
                    DateOfBirth = dob,
                    BFormCnic = $"45303-{1000000 + i:0000000}-{(i % 9) + 1}",
                    Address = $"Demo House {i}, {areas[(i - 1) % areas.Length]}, Sindh",
                    ContactNumber = $"03{20 + (i % 10):00}-{3000000 + i:0000000}",
                    AdmissionDate = admissionDate,
                    Status = StudentStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-(totalStudents - i))
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                var guardian = new Guardian
                {
                    SchoolId = schoolId,
                    FullName = fatherName,
                    Relationship = "Father",
                    Phone = $"03{10 + (i % 10):00}-{4000000 + i:0000000}",
                    Occupation = occupations[(i - 1) % occupations.Length],
                    Cnic = $"45303-{5000000 + i:0000000}-{(i % 9) + 1}",
                    Address = student.Address
                };
                db.Guardians.Add(guardian);
                await db.SaveChangesAsync();
                db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, IsPrimary = true });

                if (classIndex > 0)
                {
                    var previousClass = orderedClasses[classIndex - 1];
                    var previousSection = sections[$"{previousClass.Name}|{sectionName}"];
                    db.StudentEnrollments.Add(new StudentEnrollment
                    {
                        SchoolId = schoolId,
                        StudentId = student.Id,
                        AcademicSessionId = previousSession.Id,
                        SchoolClassId = previousClass.Id,
                        SectionId = previousSection.Id,
                        ClassName = previousClass.Name,
                        SectionName = previousSection.Name,
                        RollNumber = roll,
                        EffectiveFrom = previousSession.StartDate,
                        EffectiveTo = previousSession.EndDate,
                        IsCurrent = false,
                        Status = StudentEnrollmentStatus.Promoted,
                        Notes = "Historical demo enrollment"
                    });
                }

                int? groupId = null;
                string? groupName = null;
                if (schoolClass.Name is "Class 9" or "Class 10")
                {
                    groupName = i % 2 == 0 ? "Science" : "Computer Science";
                    groupId = groups[$"{schoolClass.Name}|{groupName}"].Id;
                }

                db.StudentEnrollments.Add(new StudentEnrollment
                {
                    SchoolId = schoolId,
                    StudentId = student.Id,
                    AcademicSessionId = activeSession.Id,
                    SchoolClassId = schoolClass.Id,
                    SectionId = section.Id,
                    AcademicGroupId = groupId,
                    ClassName = schoolClass.Name,
                    SectionName = section.Name,
                    GroupStream = groupName,
                    RollNumber = roll,
                    EffectiveFrom = activeSession.StartDate,
                    IsCurrent = true,
                    Status = StudentEnrollmentStatus.Active,
                    Notes = "Current demo enrollment"
                });
                await db.SaveChangesAsync();
            }

            var enrollment = await db.StudentEnrollments
                .FirstAsync(x => x.StudentId == student.Id && x.AcademicSessionId == activeSession.Id && x.IsCurrent);
            rows.Add(new StudentSeedRow(student, enrollment, schoolClass, section));
        }

        var counter = await db.AdmissionNumberCounters.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.AdmissionYear == currentSchoolYearStart);
        if (counter is null)
        {
            db.AdmissionNumberCounters.Add(new AdmissionNumberCounter
            {
                SchoolId = schoolId,
                AdmissionYear = currentSchoolYearStart,
                LastNumber = totalStudents
            });
        }
        else if (counter.LastNumber < totalStudents)
        {
            counter.LastNumber = totalStudents;
        }
        await db.SaveChangesAsync();
        return rows;

        static DateTime todayForBirth(DateTime sessionStart, int age, int index)
        {
            var year = sessionStart.Year - age;
            var month = 1 + (index % 12);
            var day = 1 + (index % 24);
            return new DateTime(year, month, day);
        }
    }

    private static async Task SeedAdmissionPipelineAsync(
        ApplicationDbContext db,
        int schoolId,
        int sessionId,
        DateTime today,
        string? principalId)
    {
        if (await db.AdmissionEnquiries.AnyAsync(x => x.SchoolId == schoolId && x.Notes == "Demo admission pipeline"))
            return;

        var names = new[] { "Ali Hassan", "Areeba Memon", "Danish Ali", "Eman Shaikh", "Saad Ahmed", "Noor Fatima", "Taha Khan", "Hafsa Qureshi" };
        var desired = new[] { "KG", "Class 1", "Class 6", "Class 3", "Class 9", "Nursery", "Class 7", "Class 4" };
        var stages = new[]
        {
            AdmissionEnquiryStage.New,
            AdmissionEnquiryStage.Contacted,
            AdmissionEnquiryStage.VisitScheduled,
            AdmissionEnquiryStage.TestScheduled,
            AdmissionEnquiryStage.Approved,
            AdmissionEnquiryStage.Rejected,
            AdmissionEnquiryStage.Contacted,
            AdmissionEnquiryStage.New
        };

        var enquiryRows = new List<AdmissionEnquiry>();
        for (var i = 0; i < names.Length; i++)
        {
            var row = new AdmissionEnquiry
            {
                SchoolId = schoolId,
                AcademicSessionId = sessionId,
                StudentName = names[i],
                ParentGuardianName = $"Guardian of {names[i]}",
                ContactNumber = $"0312-{6000000 + i:0000000}",
                DesiredClass = desired[i],
                SourceReferral = i % 2 == 0 ? "Parent Referral" : "Walk-in",
                FollowUpDate = today.AddDays(i + 1),
                Stage = stages[i],
                Notes = "Demo admission pipeline",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-(8 - i))
            };
            db.AdmissionEnquiries.Add(row);
            enquiryRows.Add(row);
        }
        await db.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            var enquiry = enquiryRows[i];
            db.AdmissionApplications.Add(new AdmissionApplication
            {
                SchoolId = schoolId,
                AcademicSessionId = sessionId,
                AdmissionEnquiryId = enquiry.Id,
                StudentName = enquiry.StudentName,
                Gender = i % 2 == 0 ? "Male" : "Female",
                DateOfBirth = today.AddYears(-(6 + i)).AddMonths(-i),
                Address = $"Demo applicant address {i + 1}, Sanghar",
                DesiredClass = enquiry.DesiredClass,
                AdmissionDate = today,
                PreviousSchoolName = i == 0 ? null : "Demo Previous School",
                PreviousClass = i == 0 ? null : $"Class {i}",
                GuardianName = enquiry.ParentGuardianName,
                GuardianRelationship = "Father",
                GuardianPhone = enquiry.ContactNumber,
                GuardianOccupation = "Business",
                EmergencyContactName = enquiry.ParentGuardianName,
                EmergencyContactRelationship = "Father",
                EmergencyContactPhone = enquiry.ContactNumber,
                Notes = "Demo pending admission application",
                Status = i == 0 ? AdmissionApplicationStatus.Draft : AdmissionApplicationStatus.Submitted,
                CreatedByUserId = principalId,
                SubmittedAtUtc = i == 0 ? null : DateTime.UtcNow.AddDays(-i)
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedStudentAttendanceAsync(
        ApplicationDbContext db,
        int schoolId,
        int sessionId,
        List<StudentSeedRow> students,
        DateTime today,
        string? principalId)
    {
        if (await db.StudentAttendances.AnyAsync(x => x.SchoolId == schoolId && x.MarkedByUserId == "DEMO-SEED"))
            return;

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var start = monthStart.AddDays(-14);
        if (start < students.Min(x => x.Enrollment.EffectiveFrom))
            start = students.Min(x => x.Enrollment.EffectiveFrom);

        var schoolDays = EnumerateSchoolDays(start, today).TakeLast(30).ToList();
        var random = new Random(2606);

        foreach (var day in schoolDays)
        {
            foreach (var row in students)
            {
                // Leave one section unmarked today so the dashboard demonstrates an operational alert.
                if (day == today && row.SchoolClass.Name == "Class 10" && row.Section.Name == "B")
                    continue;

                var roll = random.Next(100);
                var status = roll switch
                {
                    < 86 => StudentAttendanceStatus.Present,
                    < 91 => StudentAttendanceStatus.Absent,
                    < 95 => StudentAttendanceStatus.Late,
                    < 98 => StudentAttendanceStatus.Leave,
                    _ => StudentAttendanceStatus.HalfDay
                };

                db.StudentAttendances.Add(new StudentAttendance
                {
                    SchoolId = schoolId,
                    AcademicSessionId = sessionId,
                    StudentId = row.Student.Id,
                    StudentEnrollmentId = row.Enrollment.Id,
                    SchoolClassId = row.SchoolClass.Id,
                    SectionId = row.Section.Id,
                    AttendanceDate = day,
                    Status = status,
                    Source = AttendanceSource.Manual,
                    Remarks = status == StudentAttendanceStatus.Leave ? "Parent informed — demo" : null,
                    MarkedByUserId = "DEMO-SEED",
                    MarkedAtUtc = day.AddHours(8).ToUniversalTime()
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedFeesAsync(
        ApplicationDbContext db,
        int schoolId,
        int sessionId,
        List<StudentSeedRow> students,
        Dictionary<string, SchoolClass> classes,
        DateTime today,
        string? principalId)
    {
        var feeHeadDefs = new[]
        {
            new FeeHeadSeed("TUITION", "Tuition Fee", FeeFrequency.Monthly, 3500m, 10),
            new FeeHeadSeed("COMP", "Computer Fee", FeeFrequency.Monthly, 500m, 20),
            new FeeHeadSeed("EXAM", "Examination Fee", FeeFrequency.TermBased, 1500m, 30),
            new FeeHeadSeed("ANNUAL", "Annual Fund", FeeFrequency.OneTime, 1200m, 40),
            new FeeHeadSeed("ADMISSION", "Admission Fee", FeeFrequency.OneTime, 5000m, 50)
        };

        foreach (var def in feeHeadDefs)
        {
            if (!await db.FeeHeads.AnyAsync(x => x.SchoolId == schoolId && x.Code == def.Code))
            {
                db.FeeHeads.Add(new FeeHead
                {
                    SchoolId = schoolId,
                    Code = def.Code,
                    Name = def.Name,
                    DefaultFrequency = def.Frequency,
                    DefaultAmount = def.Amount,
                    SortOrder = def.SortOrder,
                    IsActive = true
                });
            }
        }
        await db.SaveChangesAsync();
        var feeHeads = await db.FeeHeads.Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Code);

        foreach (var schoolClass in classes.Values)
        {
            var tuition = TuitionForClass(schoolClass.Name);
            await EnsureFeeStructureAsync(db, schoolId, sessionId, feeHeads["TUITION"].Id, schoolClass.Id, FeeFrequency.Monthly, tuition, "Demo class-wise tuition");
            if (schoolClass.SortOrder >= 6)
            {
                await EnsureFeeStructureAsync(db, schoolId, sessionId, feeHeads["COMP"].Id, schoolClass.Id, FeeFrequency.Monthly, 500m, "Demo computer fee");
            }
            await EnsureFeeStructureAsync(db, schoolId, sessionId, feeHeads["EXAM"].Id, schoolClass.Id, FeeFrequency.TermBased, 1500m, "Demo term examination fee");
            await EnsureFeeStructureAsync(db, schoolId, sessionId, feeHeads["ANNUAL"].Id, schoolClass.Id, FeeFrequency.OneTime, 1200m, "Demo annual fund");
        }
        await db.SaveChangesAsync();

        if (!await db.StudentDiscounts.AnyAsync(x => x.SchoolId == schoolId && x.ApprovalNote == "Demo merit/sibling concession"))
        {
            foreach (var row in students.Where((_, index) => index % 15 == 0).Take(8))
            {
                db.StudentDiscounts.Add(new StudentDiscount
                {
                    SchoolId = schoolId,
                    StudentId = row.Student.Id,
                    FeeHeadId = feeHeads["TUITION"].Id,
                    DiscountType = DiscountType.Percentage,
                    Value = 10m,
                    StartDate = new DateTime(today.Year, 4, 1),
                    EndDate = new DateTime(today.Year + 1, 3, 31),
                    ApprovalNote = "Demo merit/sibling concession",
                    IsActive = true
                });
            }
            await db.SaveChangesAsync();
        }

        if (await db.FeeChallans.AnyAsync(x => x.SchoolId == schoolId && x.CreatedByUserId == "DEMO-SEED"))
            return;

        var discounts = await db.StudentDiscounts.Where(x => x.SchoolId == schoolId && x.IsActive).ToListAsync();
        var months = Enumerable.Range(0, 6).Select(i => new DateTime(today.Year, today.Month, 1).AddMonths(-5 + i)).ToList();
        var challanCounter = 0;
        var receiptCounter = 0;
        var random = new Random(8080);

        foreach (var month in months)
        {
            var batch = new FeeChallanBatch
            {
                SchoolId = schoolId,
                AcademicSessionId = sessionId,
                BatchKey = $"DEMO-{month:yyyy-MM}",
                BillingPeriod = month.ToString("yyyy-MM"),
                Scope = FeeBatchScope.WholeSchool,
                ExpectedCount = students.Count,
                GeneratedCount = students.Count,
                SkippedCount = 0,
                Status = FeeBatchStatus.Completed,
                CreatedByUserId = "DEMO-SEED",
                CreatedAtUtc = month.AddDays(1)
            };
            db.FeeChallanBatches.Add(batch);
            await db.SaveChangesAsync();

            decimal batchTotal = 0m;
            foreach (var row in students)
            {
                challanCounter++;
                var tuition = TuitionForClass(row.SchoolClass.Name);
                var hasComputer = row.SchoolClass.SortOrder >= 6;
                var examFee = month.Month == 9 ? 1500m : 0m;
                var annualFee = month.Month == 4 ? 1200m : 0m;
                var tuitionDiscountPct = discounts.Any(x => x.StudentId == row.Student.Id) ? 10m : 0m;
                var tuitionDiscount = Math.Round(tuition * tuitionDiscountPct / 100m, 2);
                var subtotal = tuition + (hasComputer ? 500m : 0m) + examFee + annualFee;
                var net = subtotal - tuitionDiscount;
                var dueDate = new DateTime(month.Year, month.Month, Math.Min(10, DateTime.DaysInMonth(month.Year, month.Month)));
                var olderMonth = month < new DateTime(today.Year, today.Month, 1);

                var paymentRoll = random.Next(100);
                decimal paidAmount;
                if (olderMonth)
                {
                    paidAmount = paymentRoll < 78 ? net : paymentRoll < 90 ? Math.Round(net * 0.5m, 0) : 0m;
                }
                else
                {
                    paidAmount = paymentRoll < 58 ? net : paymentRoll < 78 ? Math.Round(net * 0.5m, 0) : 0m;
                }

                var lateFee = dueDate < today && paidAmount < net && random.Next(100) < 45 ? 200m : 0m;
                var total = net + lateFee;
                if (paidAmount > total) paidAmount = total;

                var status = paidAmount >= total
                    ? FeeChallanStatus.Paid
                    : paidAmount > 0m
                        ? FeeChallanStatus.PartiallyPaid
                        : dueDate < today ? FeeChallanStatus.Overdue : FeeChallanStatus.Issued;

                var challan = new FeeChallan
                {
                    SchoolId = schoolId,
                    AcademicSessionId = sessionId,
                    StudentId = row.Student.Id,
                    StudentEnrollmentId = row.Enrollment.Id,
                    FeeChallanBatchId = batch.Id,
                    ChallanNumber = $"CH-{today.Year}-{challanCounter:000000}",
                    BillingPeriod = month.ToString("yyyy-MM"),
                    BillingPeriodStart = month,
                    BillingPeriodEnd = month.AddMonths(1).AddDays(-1),
                    IssueDate = month.AddDays(1),
                    DueDate = dueDate,
                    ClassNameSnapshot = row.SchoolClass.Name,
                    SectionNameSnapshot = row.Section.Name,
                    Subtotal = subtotal,
                    DiscountTotal = tuitionDiscount,
                    LateFeeAmount = lateFee,
                    CurrentChargesTotal = total,
                    PreviousOutstandingAtIssue = 0m,
                    PaidAmount = paidAmount,
                    Status = status,
                    Version = 1,
                    IsSuperseded = false,
                    CreatedByUserId = "DEMO-SEED",
                    CreatedAtUtc = month.AddDays(1)
                };

                challan.Items.Add(new FeeChallanItem
                {
                    FeeHeadId = feeHeads["TUITION"].Id,
                    Description = "Tuition Fee",
                    Amount = tuition,
                    DiscountAmount = tuitionDiscount,
                    NetAmount = tuition - tuitionDiscount
                });
                if (hasComputer)
                {
                    challan.Items.Add(new FeeChallanItem
                    {
                        FeeHeadId = feeHeads["COMP"].Id,
                        Description = "Computer Fee",
                        Amount = 500m,
                        DiscountAmount = 0m,
                        NetAmount = 500m
                    });
                }
                if (examFee > 0m)
                {
                    challan.Items.Add(new FeeChallanItem
                    {
                        FeeHeadId = feeHeads["EXAM"].Id,
                        Description = "Examination Fee",
                        Amount = examFee,
                        DiscountAmount = 0m,
                        NetAmount = examFee
                    });
                }
                if (annualFee > 0m)
                {
                    challan.Items.Add(new FeeChallanItem
                    {
                        FeeHeadId = feeHeads["ANNUAL"].Id,
                        Description = "Annual Fund",
                        Amount = annualFee,
                        DiscountAmount = 0m,
                        NetAmount = annualFee
                    });
                }
                db.FeeChallans.Add(challan);

                if (paidAmount > 0m)
                {
                    receiptCounter++;
                    var payment = new FeePayment
                    {
                        SchoolId = schoolId,
                        StudentId = row.Student.Id,
                        ReceiptNumber = $"RC-{today.Year}-{receiptCounter:000000}",
                        PaymentDateUtc = month.AddDays(5 + random.Next(0, 10)),
                        Amount = paidAmount,
                        PaymentMethod = random.Next(100) < 78 ? FeePaymentMethod.Cash : FeePaymentMethod.BankTransfer,
                        ReferenceNumber = random.Next(100) < 22 ? $"DEMO-TXN-{month:yyMM}-{receiptCounter:0000}" : null,
                        Notes = "Demo fee payment",
                        ReceivedByUserId = principalId,
                        CreatedAtUtc = month.AddDays(5)
                    };
                    db.FeePayments.Add(payment);
                    db.FeePaymentAllocations.Add(new FeePaymentAllocation
                    {
                        FeePayment = payment,
                        FeeChallan = challan,
                        FeeChallanItemId = null,
                        Amount = paidAmount
                    });
                }

                batchTotal += total;
            }

            batch.TotalAmount = batchTotal;
            await db.SaveChangesAsync();
        }

        await UpsertFinancialCounterAsync(db, schoolId, FinancialNumberType.Challan, today.Year, challanCounter);
        await UpsertFinancialCounterAsync(db, schoolId, FinancialNumberType.Receipt, today.Year, receiptCounter);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureFeeStructureAsync(
        ApplicationDbContext db,
        int schoolId,
        int sessionId,
        int feeHeadId,
        int classId,
        FeeFrequency frequency,
        decimal amount,
        string notes)
    {
        if (!await db.FeeStructures.AnyAsync(x => x.AcademicSessionId == sessionId && x.SchoolClassId == classId && x.FeeHeadId == feeHeadId && x.IsActive))
        {
            db.FeeStructures.Add(new FeeStructure
            {
                SchoolId = schoolId,
                AcademicSessionId = sessionId,
                FeeHeadId = feeHeadId,
                Scope = FeeStructureScope.Class,
                SchoolClassId = classId,
                Frequency = frequency,
                Amount = amount,
                EffectiveFrom = new DateTime(DateTime.Today.Year, 4, 1),
                Notes = notes,
                IsActive = true
            });
        }
    }

    private static async Task UpsertFinancialCounterAsync(
        ApplicationDbContext db,
        int schoolId,
        FinancialNumberType type,
        int year,
        int lastNumber)
    {
        var counter = await db.FinancialNumberCounters.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.NumberType == type && x.Year == year);
        if (counter is null)
        {
            db.FinancialNumberCounters.Add(new FinancialNumberCounter
            {
                SchoolId = schoolId,
                NumberType = type,
                Year = year,
                LastNumber = lastNumber
            });
        }
        else if (counter.LastNumber < lastNumber)
        {
            counter.LastNumber = lastNumber;
        }
    }

    private static decimal TuitionForClass(string className) => className switch
    {
        "Nursery" or "KG" => 3000m,
        "Class 1" or "Class 2" or "Class 3" or "Class 4" or "Class 5" => 3500m,
        "Class 6" or "Class 7" or "Class 8" => 4000m,
        _ => 4500m
    };

    private static async Task SeedExamsAndResultsAsync(
        ApplicationDbContext db,
        int schoolId,
        AcademicSession activeSession,
        List<StudentSeedRow> students,
        Dictionary<string, SchoolClass> classes,
        Dictionary<string, Section> sections,
        Dictionary<string, Subject> subjects,
        DateTime today,
        string? principalId)
    {
        if (await db.Exams.AnyAsync(x => x.SchoolId == schoolId && x.Title == "First Term Examination 2026"))
            return;

        var firstTerm = await db.Terms.OrderBy(x => x.DisplayOrder).FirstOrDefaultAsync(x => x.AcademicSessionId == activeSession.Id);
        var exam = new Exam
        {
            SchoolId = schoolId,
            AcademicSessionId = activeSession.Id,
            TermId = firstTerm?.Id,
            Title = "First Term Examination 2026",
            ExamType = "Term Examination",
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(-3),
            Status = ExamStatus.Published,
            IsActive = true,
            CreatedByUserId = principalId,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-20)
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        var examClasses = classes.Values.Where(x => x.SortOrder >= 3).OrderBy(x => x.SortOrder).ToList();
        foreach (var schoolClass in examClasses)
        {
            db.ExamClasses.Add(new ExamClass
            {
                SchoolId = schoolId,
                ExamId = exam.Id,
                SchoolClassId = schoolClass.Id
            });
        }
        await db.SaveChangesAsync();

        var examSubjectMap = new Dictionary<(int ClassId, string Code), ExamSubject>();
        foreach (var schoolClass in examClasses)
        {
            foreach (var code in SubjectCodesForClass(schoolClass.Name).Take(schoolClass.SortOrder >= 11 ? 8 : 6))
            {
                var subject = subjects[code];
                var examSubject = new ExamSubject
                {
                    SchoolId = schoolId,
                    ExamId = exam.Id,
                    SchoolClassId = schoolClass.Id,
                    SubjectId = subject.Id,
                    MaxMarks = 100m,
                    PassMarks = 40m,
                    TheoryMaxMarks = subject.HasPractical ? 80m : 100m,
                    PracticalMaxMarks = subject.HasPractical ? 20m : null,
                    WeightagePercent = 100m,
                    IsActive = true
                };
                db.ExamSubjects.Add(examSubject);
                examSubjectMap[(schoolClass.Id, code)] = examSubject;
            }
        }
        await db.SaveChangesAsync();

        var random = new Random(9090);
        var resultDrafts = new List<ResultDraft>();

        foreach (var schoolClass in examClasses)
        {
            var classStudents = students.Where(x => x.SchoolClass.Id == schoolClass.Id).ToList();
            var subjectRows = examSubjectMap.Where(x => x.Key.ClassId == schoolClass.Id).Select(x => x.Value).ToList();

            var marksSheetsBySubjectAndSection = new Dictionary<(int SubjectId, int SectionId), ExamMarksSheet>();
            foreach (var examSubject in subjectRows)
            {
                foreach (var sectionName in new[] { "A", "B" })
                {
                    var section = sections[$"{schoolClass.Name}|{sectionName}"];
                    var sheet = new ExamMarksSheet
                    {
                        SchoolId = schoolId,
                        ExamId = exam.Id,
                        ExamSubjectId = examSubject.Id,
                        SectionId = section.Id,
                        Status = ExamMarksSheetStatus.Locked,
                        SubmittedByUserId = principalId,
                        SubmittedAtUtc = DateTime.UtcNow.AddDays(-4),
                        VerifiedByUserId = principalId,
                        VerifiedAtUtc = DateTime.UtcNow.AddDays(-3),
                        LockedByUserId = principalId,
                        LockedAtUtc = DateTime.UtcNow.AddDays(-2)
                    };
                    db.ExamMarksSheets.Add(sheet);
                    marksSheetsBySubjectAndSection[(examSubject.Id, section.Id)] = sheet;
                }
            }
            await db.SaveChangesAsync();

            foreach (var studentRow in classStudents)
            {
                decimal totalObtained = 0m;
                decimal totalMaximum = 0m;
                var passed = true;

                foreach (var examSubject in subjectRows)
                {
                    var absent = random.Next(100) < 2;
                    decimal? obtained = null;
                    decimal? theory = null;
                    decimal? practical = null;

                    if (!absent)
                    {
                        var baseMark = 45 + random.Next(0, 51);
                        obtained = Math.Min(100m, baseMark);
                        if (examSubject.PracticalMaxMarks.HasValue)
                        {
                            practical = Math.Min(20m, Math.Round(obtained.Value * 0.2m, 0));
                            theory = obtained - practical;
                        }
                        else
                        {
                            theory = obtained;
                        }
                    }

                    var sheet = marksSheetsBySubjectAndSection[(examSubject.Id, studentRow.Section.Id)];
                    db.StudentMarks.Add(new StudentMark
                    {
                        SchoolId = schoolId,
                        ExamId = exam.Id,
                        ExamSubjectId = examSubject.Id,
                        ExamMarksSheetId = sheet.Id,
                        StudentId = studentRow.Student.Id,
                        StudentEnrollmentId = studentRow.Enrollment.Id,
                        SpecialStatus = absent ? MarkSpecialStatus.Absent : MarkSpecialStatus.None,
                        TheoryMarks = theory,
                        PracticalMarks = practical,
                        ObtainedMarks = obtained,
                        TeacherRemarks = absent ? "Absent — demo" : null,
                        EnteredByUserId = principalId,
                        EnteredAtUtc = DateTime.UtcNow.AddDays(-5)
                    });

                    totalMaximum += examSubject.MaxMarks;
                    totalObtained += obtained ?? 0m;
                    if (!obtained.HasValue || obtained.Value < examSubject.PassMarks)
                    {
                        passed = false;
                    }
                }

                var percentage = totalMaximum == 0m ? 0m : Math.Round(totalObtained * 100m / totalMaximum, 2);
                resultDrafts.Add(new ResultDraft(studentRow, totalObtained, totalMaximum, percentage, passed));
            }
        }
        await db.SaveChangesAsync();

        foreach (var classGroup in resultDrafts.GroupBy(x => x.Row.SchoolClass.Id))
        {
            var ordered = classGroup.OrderByDescending(x => x.Percentage).ThenBy(x => x.Row.Student.FullName).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                var result = ordered[i];
                var attendance = await db.StudentAttendances
                    .Where(x => x.StudentId == result.Row.Student.Id && x.AcademicSessionId == activeSession.Id &&
                                x.Status != StudentAttendanceStatus.Holiday && x.Status != StudentAttendanceStatus.NoClass)
                    .ToListAsync();
                var attendancePct = attendance.Count == 0
                    ? 0m
                    : Math.Round(attendance.Count(x => x.Status is StudentAttendanceStatus.Present or StudentAttendanceStatus.Late or StudentAttendanceStatus.HalfDay) * 100m / attendance.Count, 2);

                db.StudentResults.Add(new StudentResult
                {
                    SchoolId = schoolId,
                    AcademicSessionId = activeSession.Id,
                    ExamId = exam.Id,
                    StudentId = result.Row.Student.Id,
                    StudentEnrollmentId = result.Row.Enrollment.Id,
                    VersionNumber = 1,
                    Status = StudentResultStatus.Published,
                    IsCurrent = true,
                    ObtainedMarks = result.Obtained,
                    MaximumMarks = result.Maximum,
                    Percentage = result.Percentage,
                    Grade = result.Passed ? GradeFor(result.Percentage) : "F",
                    IsPassed = result.Passed,
                    ClassPosition = i + 1,
                    AttendancePercentage = attendancePct,
                    TeacherRemarks = result.Passed ? "Keep up the good work." : "Needs additional academic support.",
                    PublishedByUserId = principalId,
                    PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                });
            }
        }

        db.Exams.Add(new Exam
        {
            SchoolId = schoolId,
            AcademicSessionId = activeSession.Id,
            Title = "Second Term / Pre-Board Assessment",
            ExamType = "Assessment",
            StartDate = today.AddDays(35),
            EndDate = today.AddDays(42),
            Status = ExamStatus.Draft,
            IsActive = true,
            CreatedByUserId = principalId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static string GradeFor(decimal percentage) => percentage switch
    {
        >= 90m => "A+",
        >= 80m => "A",
        >= 70m => "B",
        >= 60m => "C",
        >= 50m => "D",
        >= 40m => "E",
        _ => "F"
    };

    private static async Task SeedStaffAttendanceAndPayrollAsync(
        ApplicationDbContext db,
        int schoolId,
        List<Staff> staff,
        DateTime today,
        string? principalId)
    {
        if (!await db.StaffAttendances.AnyAsync(x => x.SchoolId == schoolId && x.MarkedByUserId == "DEMO-SEED"))
        {
            var random = new Random(1212);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            foreach (var day in EnumerateSchoolDays(monthStart, today))
            {
                foreach (var person in staff.Where(x => x.Status == StaffStatus.Active))
                {
                    var roll = random.Next(100);
                    var status = roll switch
                    {
                        < 90 => StaffAttendanceStatus.Present,
                        < 94 => StaffAttendanceStatus.Late,
                        < 97 => StaffAttendanceStatus.Leave,
                        < 99 => StaffAttendanceStatus.Absent,
                        _ => StaffAttendanceStatus.HalfDay
                    };
                    db.StaffAttendances.Add(new StaffAttendance
                    {
                        SchoolId = schoolId,
                        StaffId = person.Id,
                        AttendanceDate = day,
                        Status = status,
                        Source = AttendanceSource.Manual,
                        CheckInUtc = status == StaffAttendanceStatus.Absent ? null : day.AddHours(8).AddMinutes(random.Next(0, 35)).ToUniversalTime(),
                        CheckOutUtc = status == StaffAttendanceStatus.Absent ? null : day.AddHours(14).AddMinutes(random.Next(0, 45)).ToUniversalTime(),
                        Remarks = status == StaffAttendanceStatus.Leave ? "Approved leave — demo" : null,
                        MarkedByUserId = "DEMO-SEED"
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        foreach (var month in new[] { new DateTime(today.Year, today.Month, 1).AddMonths(-1), new DateTime(today.Year, today.Month, 1) })
        {
            if (await db.PayrollRuns.AnyAsync(x => x.SchoolId == schoolId && x.PeriodYear == month.Year && x.PeriodMonth == month.Month))
                continue;

            var run = new PayrollRun
            {
                SchoolId = schoolId,
                PeriodYear = month.Year,
                PeriodMonth = month.Month,
                Status = PayrollRunStatus.Posted,
                CreatedByUserId = principalId,
                CreatedAtUtc = month.AddMonths(1).AddDays(-1),
                ValidatedByUserId = principalId,
                ValidatedAtUtc = month.AddMonths(1).AddDays(-2),
                ApprovedByUserId = principalId,
                ApprovedAtUtc = month.AddMonths(1).AddDays(-1),
                PostedByUserId = principalId,
                PostedAtUtc = month.AddMonths(1).AddDays(-1),
                PaymentMethod = PayrollPaymentMethod.BankTransfer,
                PaymentReference = $"DEMO-PAY-{month:yyyyMM}",
                Notes = "Demo monthly payroll"
            };
            db.PayrollRuns.Add(run);
            await db.SaveChangesAsync();

            foreach (var person in staff.Where(x => x.Status == StaffStatus.Active))
            {
                var structure = await db.SalaryStructures.FirstAsync(x => x.Id == person.SalaryStructureId);
                var monthEnd = month.AddMonths(1);
                var attendance = await db.StaffAttendances
                    .Where(x => x.StaffId == person.Id && x.AttendanceDate >= month && x.AttendanceDate < monthEnd)
                    .ToListAsync();

                var present = attendance.Count(x => x.Status == StaffAttendanceStatus.Present);
                var absent = attendance.Count(x => x.Status == StaffAttendanceStatus.Absent);
                var late = attendance.Count(x => x.Status == StaffAttendanceStatus.Late);
                var leave = attendance.Count(x => x.Status == StaffAttendanceStatus.Leave);
                var half = attendance.Count(x => x.Status == StaffAttendanceStatus.HalfDay);
                var allowances = structure.HouseAllowance + structure.MedicalAllowance + structure.TransportAllowance + structure.OtherAllowance;
                var gross = structure.BasicSalary + allowances;
                var attendanceDeduction = absent * structure.AbsenceDeductionPerDay + half * structure.HalfDayDeductionPerDay + late * structure.LateDeductionPerOccurrence;
                var totalDeduction = structure.FixedDeduction + attendanceDeduction;
                var net = Math.Max(0m, gross - totalDeduction);

                db.PayrollItems.Add(new PayrollItem
                {
                    SchoolId = schoolId,
                    PayrollRunId = run.Id,
                    StaffId = person.Id,
                    SalaryStructureId = structure.Id,
                    EmployeeIdSnapshot = person.EmployeeId,
                    StaffNameSnapshot = person.FullName,
                    DepartmentSnapshot = person.Department,
                    BasicSalary = structure.BasicSalary,
                    FixedAllowances = allowances,
                    ManualAllowance = 0m,
                    GrossPay = gross,
                    PresentDays = present,
                    AbsentDays = absent,
                    LateDays = late,
                    LeaveDays = leave,
                    HalfDays = half,
                    AttendanceDeduction = attendanceDeduction,
                    FixedDeduction = structure.FixedDeduction,
                    AdvanceDeduction = 0m,
                    ManualDeduction = 0m,
                    TotalDeductions = totalDeduction,
                    NetPay = net,
                    IsValid = true
                });
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedExpensesAndIncomeAsync(
        ApplicationDbContext db,
        int schoolId,
        DateTime today,
        string? principalId)
    {
        var categoryNames = new[] { "Utilities", "Maintenance", "Stationery", "Transport", "Rent", "Internet", "Cleaning", "Events", "Miscellaneous" };
        var categories = await db.ExpenseCategories.Where(x => x.SchoolId == schoolId).ToListAsync();
        foreach (var name in categoryNames)
        {
            if (categories.All(x => x.Name != name))
            {
                db.ExpenseCategories.Add(new ExpenseCategory
                {
                    SchoolId = schoolId,
                    Name = name,
                    SortOrder = (Array.IndexOf(categoryNames, name) + 1) * 10,
                    IsActive = true
                });
            }
        }
        await db.SaveChangesAsync();
        var map = await db.ExpenseCategories.Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Name);

        if (!await db.Expenses.AnyAsync(x => x.SchoolId == schoolId && x.Notes == "Demo operating expense"))
        {
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var expenses = new[]
            {
                new ExpenseSeed("Utilities", 28500m, "HESCO Electricity Bill", 3, false),
                new ExpenseSeed("Internet", 6500m, "Internet Service", 5, false),
                new ExpenseSeed("Stationery", 18200m, "Stationery Supplier", 7, false),
                new ExpenseSeed("Cleaning", 9500m, "Cleaning Supplies", 9, false),
                new ExpenseSeed("Maintenance", 22500m, "Electric / Maintenance Work", 11, false),
                new ExpenseSeed("Transport", 12000m, "School Transport Fuel", 13, false),
                new ExpenseSeed("Events", 18000m, "School Activity Material", 15, false),
                new ExpenseSeed("Maintenance", 78000m, "Classroom Repair Estimate", 17, true)
            };

            foreach (var item in expenses)
            {
                db.Expenses.Add(new Expense
                {
                    SchoolId = schoolId,
                    ExpenseCategoryId = map[item.Category].Id,
                    ExpenseDate = monthStart.AddDays(Math.Min(item.Day, DateTime.DaysInMonth(monthStart.Year, monthStart.Month) - 1)),
                    Amount = item.Amount,
                    PaidTo = item.PaidTo,
                    PaymentMethod = item.Amount > 20000m ? ExpensePaymentMethod.BankTransfer : ExpensePaymentMethod.Cash,
                    ReferenceNumber = item.Amount > 20000m ? $"DEMO-EXP-{item.Day:00}" : null,
                    Notes = "Demo operating expense",
                    ApprovalRequired = item.PendingApproval,
                    ApprovalStatus = item.PendingApproval ? ExpenseApprovalStatus.Pending : ExpenseApprovalStatus.NotRequired,
                    CreatedByUserId = principalId
                });
            }

            var previousMonth = monthStart.AddMonths(-1);
            db.Expenses.Add(new Expense
            {
                SchoolId = schoolId,
                ExpenseCategoryId = map["Utilities"].Id,
                ExpenseDate = previousMonth.AddDays(4),
                Amount = 26300m,
                PaidTo = "HESCO Electricity Bill",
                PaymentMethod = ExpensePaymentMethod.BankTransfer,
                ReferenceNumber = "DEMO-PREV-UTIL",
                Notes = "Demo operating expense",
                ApprovalRequired = false,
                ApprovalStatus = ExpenseApprovalStatus.NotRequired,
                CreatedByUserId = principalId
            });

            db.OtherIncomes.AddRange(
                new OtherIncome
                {
                    SchoolId = schoolId,
                    IncomeDate = monthStart.AddDays(6),
                    Amount = 15000m,
                    Source = "School canteen stall rent — demo",
                    PaymentMethod = ExpensePaymentMethod.Cash,
                    Notes = "Demo other income",
                    CreatedByUserId = principalId
                },
                new OtherIncome
                {
                    SchoolId = schoolId,
                    IncomeDate = monthStart.AddDays(12),
                    Amount = 8000m,
                    Source = "Activity contribution — demo",
                    PaymentMethod = ExpensePaymentMethod.BankTransfer,
                    ReferenceNumber = "DEMO-OTHER-002",
                    Notes = "Demo other income",
                    CreatedByUserId = principalId
                });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedNoticesAsync(
        ApplicationDbContext db,
        int schoolId,
        Dictionary<string, SchoolClass> classes,
        Dictionary<string, Section> sections,
        DateTime today,
        string? principalId)
    {
        if (await db.Notices.AnyAsync(x => x.SchoolId == schoolId && x.Body.Contains("Demo notice")))
            return;

        db.Notices.AddRange(
            new Notice
            {
                SchoolId = schoolId,
                Title = "Parent-Teacher Meeting",
                Body = "Demo notice: Parent-Teacher Meeting will be held on Saturday from 9:00 AM to 12:00 PM.",
                Audience = NoticeAudience.Parents,
                IsPublished = true,
                PublishFromUtc = DateTime.UtcNow.AddDays(-2),
                PublishedAtUtc = DateTime.UtcNow.AddDays(-2),
                CreatedByUserId = principalId
            },
            new Notice
            {
                SchoolId = schoolId,
                Title = "Monthly Fee Reminder",
                Body = "Demo notice: Parents are requested to clear outstanding fee dues through the school accounts office.",
                Audience = NoticeAudience.Parents,
                IsPublished = true,
                PublishFromUtc = DateTime.UtcNow.AddDays(-1),
                PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
                CreatedByUserId = principalId
            },
            new Notice
            {
                SchoolId = schoolId,
                Title = "Second Term Assessment Preparation",
                Body = "Demo notice: Students should follow the issued syllabus and prepare for the upcoming assessment.",
                Audience = NoticeAudience.Students,
                IsPublished = true,
                PublishFromUtc = DateTime.UtcNow,
                PublishedAtUtc = DateTime.UtcNow,
                CreatedByUserId = principalId
            },
            new Notice
            {
                SchoolId = schoolId,
                Title = "Staff Coordination Meeting",
                Body = "Demo notice: Teaching staff meeting is scheduled after school hours in the staff room.",
                Audience = NoticeAudience.Staff,
                IsPublished = true,
                PublishFromUtc = DateTime.UtcNow,
                PublishedAtUtc = DateTime.UtcNow,
                CreatedByUserId = principalId
            },
            new Notice
            {
                SchoolId = schoolId,
                Title = "Class 10 Academic Review",
                Body = "Demo notice: Class 10 students should review first-term performance with their class teacher.",
                Audience = NoticeAudience.Class,
                SchoolClassId = classes["Class 10"].Id,
                SectionId = sections["Class 10|A"].Id,
                IsPublished = true,
                PublishFromUtc = DateTime.UtcNow,
                PublishedAtUtc = DateTime.UtcNow,
                CreatedByUserId = principalId
            });
        await db.SaveChangesAsync();
    }

    private static async Task SeedAuditTrailAsync(
        ApplicationDbContext db,
        int schoolId,
        DateTime today,
        string? userEmail)
    {
        if (await db.AuditLogs.AnyAsync(x => x.SchoolId == schoolId && x.Action == "Demo.Activity"))
            return;

        var actions = new[]
        {
            ("FeePayment.Posted", "FeePayment", "Demo receipt posted"),
            ("Attendance.Marked", "StudentAttendance", "Class attendance submitted"),
            ("Result.Published", "StudentResult", "First term results published"),
            ("Expense.Created", "Expense", "Operating expense recorded"),
            ("Payroll.Posted", "PayrollRun", "Monthly payroll posted"),
            ("Notice.Published", "Notice", "Parent notice published"),
            ("Student.Updated", "Student", "Student profile updated")
        };

        for (var i = 0; i < actions.Length; i++)
        {
            db.AuditLogs.Add(new AuditLog
            {
                SchoolId = schoolId,
                UserEmail = userEmail ?? "principal@school.local",
                Action = i == 0 ? "Demo.Activity" : actions[i].Item1,
                EntityType = actions[i].Item2,
                EntityId = $"DEMO-{i + 1}",
                Details = actions[i].Item3,
                IpAddress = "127.0.0.1",
                UserAgent = "Demo data seeder",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-(actions.Length - i) * 12)
            });
        }
        await db.SaveChangesAsync();
    }

    private static IEnumerable<DateTime> EnumerateSchoolDays(DateTime start, DateTime end)
    {
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek != DayOfWeek.Sunday)
                yield return day;
        }
    }

    private sealed record SalarySeed(string Name, decimal Basic, decimal House, decimal Medical, decimal Transport, decimal Other);
    private sealed record StaffSeed(string EmployeeId, string FullName, string Designation, string Department, string Qualification, string SalaryPackage, string? Role, string? Email);
    private sealed record StaffSeedContext(List<Staff> StaffMembers, List<ApplicationUser> TeacherUsers);
    private sealed record StudentSeedRow(Student Student, StudentEnrollment Enrollment, SchoolClass SchoolClass, Section Section);
    private sealed record ResultDraft(StudentSeedRow Row, decimal Obtained, decimal Maximum, decimal Percentage, bool Passed);
    private sealed record FeeHeadSeed(string Code, string Name, FeeFrequency Frequency, decimal Amount, int SortOrder);
    private sealed record ExpenseSeed(string Category, decimal Amount, string PaidTo, int Day, bool PendingApproval);
}
