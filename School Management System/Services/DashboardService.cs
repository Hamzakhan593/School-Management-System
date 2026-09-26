using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly ISchoolContextService _schoolContext;

    public DashboardService(ApplicationDbContext db, ISchoolContextService schoolContext)
    {
        _db = db;
        _schoolContext = schoolContext;
    }

    public async Task<DashboardViewModel> BuildAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default)
    {
        var model = new DashboardViewModel
        {
            UserName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "User" : user.FullName,
            RoleName = GetRoleDisplayName(role),
            GeneratedAt = DateTime.Now,
            ShowAdmissions = role is AppRoles.SuperAdmin or AppRoles.Principal or AppRoles.Admin or AppRoles.Receptionist,
            ShowAttendance = role is AppRoles.SuperAdmin or AppRoles.Principal or AppRoles.Admin or AppRoles.Teacher,
            ShowFinance = role is AppRoles.SuperAdmin or AppRoles.Principal or AppRoles.Admin or AppRoles.Accountant,
            ShowAcademics = role is AppRoles.SuperAdmin or AppRoles.Principal or AppRoles.Admin or AppRoles.ExamController or AppRoles.Teacher,
            ShowHr = role is AppRoles.SuperAdmin or AppRoles.Principal or AppRoles.Admin or AppRoles.HR,
            ShowAudit = role is AppRoles.SuperAdmin or AppRoles.Principal or AppRoles.Admin
        };

        var school = await _schoolContext.GetCurrentSchoolAsync();
        if (school is null)
        {
            model.Alerts.Add(new DashboardAlertItem
            {
                Title = "School setup required",
                Message = "Create the school profile before operational dashboard data can be displayed.",
                Tone = "warning",
                Controller = "SchoolSetup",
                Action = "Index"
            });
            return model;
        }

        model.HasSchoolContext = true;
        model.SchoolName = school.Name;
        model.SchoolLogoPath = school.LogoPath;
        var schoolId = school.Id;
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var activeSession = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
        model.AcademicSessionName = activeSession?.Name ?? "No active session";
        var activeSessionId = activeSession?.Id;

        model.ActiveStudents = await _db.Students.AsNoTracking()
            .CountAsync(x => x.SchoolId == schoolId && x.Status == StudentStatus.Active, cancellationToken);
        model.ActiveStaff = await _db.Staff.AsNoTracking()
            .CountAsync(x => x.SchoolId == schoolId && x.Status == StaffStatus.Active, cancellationToken);

        if (model.ShowAdmissions)
        {
            model.NewAdmissionsThisMonth = await _db.Students.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && x.AdmissionDate >= monthStart && x.AdmissionDate < nextMonth, cancellationToken);
            model.OpenEnquiries = await _db.AdmissionEnquiries.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && x.Stage != AdmissionEnquiryStage.Rejected && x.Stage != AdmissionEnquiryStage.Admitted, cancellationToken);
            model.PendingAdmissions = await _db.AdmissionApplications.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && (x.Status == AdmissionApplicationStatus.Draft || x.Status == AdmissionApplicationStatus.Submitted), cancellationToken);

            var recentAdmissions = await _db.Students.AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(6)
                .Select(x => new
                {
                    x.FullName,
                    x.AdmissionNumber,
                    x.AdmissionDate,
                    ClassName = x.Enrollments.Where(e => e.IsCurrent).Select(e => e.ClassName).FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            model.RecentAdmissions = recentAdmissions.Select(x => new DashboardAdmissionItem
            {
                StudentName = x.FullName,
                AdmissionNumber = x.AdmissionNumber,
                Date = x.AdmissionDate,
                ClassName = x.ClassName ?? "Not assigned"
            }).ToList();
        }

        if (model.ShowAttendance)
        {
            var todayAttendance = _db.StudentAttendances.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.AttendanceDate == today);
            if (activeSessionId.HasValue)
            {
                todayAttendance = todayAttendance.Where(x => x.AcademicSessionId == activeSessionId.Value);
            }

            var expectedStudents = _db.StudentEnrollments.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsCurrent && x.Status == StudentEnrollmentStatus.Active && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today));
            if (activeSessionId.HasValue) expectedStudents = expectedStudents.Where(x => x.AcademicSessionId == activeSessionId.Value);
            if (role == AppRoles.Teacher)
            {
                var assignments = _db.TeacherAssignments.Where(x => x.SchoolId == schoolId && x.TeacherUserId == user.Id && x.IsActive);
                expectedStudents = expectedStudents.Where(e => assignments.Any(a => a.AcademicSessionId == e.AcademicSessionId && a.SchoolClassId == e.SchoolClassId && (a.SectionId == null || a.SectionId == e.SectionId)) || _db.Sections.Any(s => s.SchoolId == schoolId && s.Id == e.SectionId && s.ClassTeacherUserId == user.Id && s.IsActive));
                todayAttendance = todayAttendance.Where(t => expectedStudents.Any(e => e.StudentId == t.StudentId));
            }
            model.TodayUnmarked = await expectedStudents.Select(x => x.StudentId).Distinct().CountAsync(id => !todayAttendance.Any(a => a.StudentId == id), cancellationToken);
            model.TodayLeave = await todayAttendance.CountAsync(x => x.Status == StudentAttendanceStatus.Leave, cancellationToken);
            model.TodayPresent = await todayAttendance.CountAsync(x => x.Status == StudentAttendanceStatus.Present || x.Status == StudentAttendanceStatus.HalfDay, cancellationToken);
            model.TodayAbsent = await todayAttendance.CountAsync(x => x.Status == StudentAttendanceStatus.Absent, cancellationToken);
            model.TodayLate = await todayAttendance.CountAsync(x => x.Status == StudentAttendanceStatus.Late, cancellationToken);
            var attendanceDenominator = await todayAttendance.CountAsync(x =>
                x.Status != StudentAttendanceStatus.Holiday && x.Status != StudentAttendanceStatus.NoClass, cancellationToken);
            model.TodayAttendancePercentage = attendanceDenominator == 0
                ? 0m
                : Math.Round((model.TodayPresent + model.TodayLate) * 100m / attendanceDenominator, 1);

            var enrollmentSections = _db.StudentEnrollments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.IsCurrent && x.Status == StudentEnrollmentStatus.Active && x.SectionId.HasValue);
            if (activeSessionId.HasValue)
            {
                enrollmentSections = enrollmentSections.Where(x => x.AcademicSessionId == activeSessionId.Value);
            }
            var expectedSectionIds = await enrollmentSections.Select(x => x.SectionId!.Value).Distinct().ToListAsync(cancellationToken);
            var markedSectionIds = await todayAttendance.Where(x => x.SectionId.HasValue).Select(x => x.SectionId!.Value).Distinct().ToListAsync(cancellationToken);
            model.UnmarkedSections = expectedSectionIds.Except(markedSectionIds).Count();

            var monthlyAttendance = _db.StudentAttendances.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.AttendanceDate >= monthStart && x.AttendanceDate < nextMonth &&
                            x.Status != StudentAttendanceStatus.Holiday && x.Status != StudentAttendanceStatus.NoClass);
            if (activeSessionId.HasValue)
            {
                monthlyAttendance = monthlyAttendance.Where(x => x.AcademicSessionId == activeSessionId.Value);
            }
            var attendanceByStudent = await monthlyAttendance
                .GroupBy(x => x.StudentId)
                .Select(g => new
                {
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == StudentAttendanceStatus.Present),
                    Late = g.Count(x => x.Status == StudentAttendanceStatus.Late),
                    HalfDay = g.Count(x => x.Status == StudentAttendanceStatus.HalfDay)
                })
                .ToListAsync(cancellationToken);
            model.LowAttendanceStudents = attendanceByStudent.Count(x => x.Total > 0 && ((x.Present + x.Late + (x.HalfDay * 0.5m)) * 100m / x.Total) < 75m);

            for (var i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var dayQuery = _db.StudentAttendances.AsNoTracking()
                    .Where(x => x.SchoolId == schoolId && x.AttendanceDate == day && x.Status != StudentAttendanceStatus.Holiday && x.Status != StudentAttendanceStatus.NoClass);
                if (activeSessionId.HasValue)
                {
                    dayQuery = dayQuery.Where(x => x.AcademicSessionId == activeSessionId.Value);
                }
                var total = await dayQuery.CountAsync(cancellationToken);
                var attended = await dayQuery.CountAsync(x => x.Status == StudentAttendanceStatus.Present || x.Status == StudentAttendanceStatus.Late || x.Status == StudentAttendanceStatus.HalfDay, cancellationToken);
                model.AttendanceTrend.Add(new DashboardTrendPoint
                {
                    Label = day.ToString("ddd"),
                    PrimaryValue = total == 0 ? 0m : Math.Round(attended * 100m / total, 1)
                });
            }
        }

        var classStrengthQuery = _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsCurrent && x.Status == StudentEnrollmentStatus.Active);
        if (activeSessionId.HasValue)
        {
            classStrengthQuery = classStrengthQuery.Where(x => x.AcademicSessionId == activeSessionId.Value);
        }
        model.ClassStrength = await classStrengthQuery
            .GroupBy(x => x.ClassName)
            .Select(g => new DashboardBarItem { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (model.ShowFinance)
        {
            var monthChallans = _db.FeeChallans.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.BillingPeriodStart >= monthStart && x.BillingPeriodStart < nextMonth && !x.IsSuperseded && x.Status != FeeChallanStatus.Cancelled);
            model.CurrentMonthExpectedFees = await monthChallans.SumAsync(x => (decimal?)x.CurrentChargesTotal, cancellationToken) ?? 0m;
            model.CurrentMonthCollectedFees = await _db.FeePayments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && !x.IsReversed && x.PaymentDateUtc >= monthStart && x.PaymentDateUtc < nextMonth)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
            model.OutstandingFees = await _db.FeeChallans.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && !x.IsSuperseded && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived && x.CurrentChargesTotal > x.PaidAmount)
                .SumAsync(x => (decimal?)(x.CurrentChargesTotal - x.PaidAmount), cancellationToken) ?? 0m;
            model.OverdueFees = await _db.FeeChallans.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && !x.IsSuperseded && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived && x.DueDate < today && x.CurrentChargesTotal > x.PaidAmount)
                .SumAsync(x => (decimal?)(x.CurrentChargesTotal - x.PaidAmount), cancellationToken) ?? 0m;
            model.CollectionRate = model.CurrentMonthExpectedFees <= 0 ? 0m : Math.Min(100m, Math.Round(model.CurrentMonthCollectedFees * 100m / model.CurrentMonthExpectedFees, 1));
            model.FeeDefaulters = await _db.FeeChallans.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && !x.IsSuperseded && x.Status != FeeChallanStatus.Cancelled && x.Status != FeeChallanStatus.Waived && x.DueDate < today && x.CurrentChargesTotal > x.PaidAmount)
                .Select(x => x.StudentId).Distinct().CountAsync(cancellationToken);

            model.CurrentMonthExpenses = await _db.Expenses.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && !x.IsCancelled && x.ExpenseDate >= monthStart && x.ExpenseDate < nextMonth &&
                            (x.ApprovalStatus == ExpenseApprovalStatus.NotRequired || x.ApprovalStatus == ExpenseApprovalStatus.Approved))
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
            model.CurrentMonthPayroll = await _db.PayrollItems.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.PayrollRun.PeriodYear == today.Year && x.PayrollRun.PeriodMonth == today.Month && x.PayrollRun.Status == PayrollRunStatus.Posted)
                .SumAsync(x => (decimal?)x.NetPay, cancellationToken) ?? 0m;
            model.PendingExpenseApprovals = await _db.Expenses.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && !x.IsCancelled && x.ApprovalStatus == ExpenseApprovalStatus.Pending, cancellationToken);
            model.PendingPayrollRuns = await _db.PayrollRuns.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && (x.Status == PayrollRunStatus.Validated || x.Status == PayrollRunStatus.Approved), cancellationToken);

            var recentPayments = await _db.FeePayments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && !x.IsReversed)
                .OrderByDescending(x => x.PaymentDateUtc)
                .Take(6)
                .Select(x => new { x.ReceiptNumber, StudentName = x.Student.FullName, x.Amount, x.PaymentDateUtc, x.PaymentMethod })
                .ToListAsync(cancellationToken);
            model.RecentPayments = recentPayments.Select(x => new DashboardPaymentItem
            {
                ReceiptNumber = x.ReceiptNumber,
                StudentName = x.StudentName,
                Amount = x.Amount,
                Date = x.PaymentDateUtc,
                Method = x.PaymentMethod.ToString()
            }).ToList();

            for (var i = 5; i >= 0; i--)
            {
                var start = monthStart.AddMonths(-i);
                var end = start.AddMonths(1);
                var expected = await _db.FeeChallans.AsNoTracking()
                    .Where(x => x.SchoolId == schoolId && x.BillingPeriodStart >= start && x.BillingPeriodStart < end && !x.IsSuperseded && x.Status != FeeChallanStatus.Cancelled)
                    .SumAsync(x => (decimal?)x.CurrentChargesTotal, cancellationToken) ?? 0m;
                var collected = await _db.FeePayments.AsNoTracking()
                    .Where(x => x.SchoolId == schoolId && !x.IsReversed && x.PaymentDateUtc >= start && x.PaymentDateUtc < end)
                    .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
                model.FeeTrend.Add(new DashboardTrendPoint { Label = start.ToString("MMM"), PrimaryValue = expected, SecondaryValue = collected });
            }

            model.ExpenseBreakdown = await _db.Expenses.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && !x.IsCancelled && x.ExpenseDate >= monthStart && x.ExpenseDate < nextMonth &&
                            (x.ApprovalStatus == ExpenseApprovalStatus.NotRequired || x.ApprovalStatus == ExpenseApprovalStatus.Approved))
                .GroupBy(x => x.ExpenseCategory.Name)
                .Select(g => new DashboardBarItem { Label = g.Key, Value = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Value)
                .Take(6)
                .ToListAsync(cancellationToken);
        }

        if (model.ShowHr && !model.ShowFinance)
        {
            model.CurrentMonthPayroll = await _db.PayrollItems.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.PayrollRun.PeriodYear == today.Year && x.PayrollRun.PeriodMonth == today.Month && x.PayrollRun.Status == PayrollRunStatus.Posted)
                .SumAsync(x => (decimal?)x.NetPay, cancellationToken) ?? 0m;
            model.PendingPayrollRuns = await _db.PayrollRuns.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && (x.Status == PayrollRunStatus.Validated || x.Status == PayrollRunStatus.Approved), cancellationToken);
        }

        if (model.ShowAcademics)
        {
            model.ActiveExams = await _db.Exams.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && x.IsActive && x.Status != ExamStatus.Archived, cancellationToken);
            model.PublishedResultsThisMonth = await _db.StudentResults.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && x.IsCurrent && x.Status == StudentResultStatus.Published && x.PublishedAtUtc >= monthStart && x.PublishedAtUtc < nextMonth, cancellationToken);
            var exams = await _db.Exams.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.IsActive && x.Status != ExamStatus.Archived)
                .OrderBy(x => x.StartDate < today ? 1 : 0)
                .ThenBy(x => x.StartDate)
                .Take(5)
                .Select(x => new { x.Id, x.Title, x.ExamType, x.StartDate, x.Status })
                .ToListAsync(cancellationToken);
            model.UpcomingExams = exams.Select(x => new DashboardExamItem
            {
                ExamId = x.Id,
                Title = x.Title,
                Type = x.ExamType,
                StartDate = x.StartDate,
                Status = x.Status.ToString()
            }).ToList();
        }

        if (role == AppRoles.Teacher)
        {
            model.TeacherAssignments = await _db.TeacherAssignments.AsNoTracking()
                .CountAsync(x => x.SchoolId == schoolId && x.TeacherUserId == user.Id && x.IsActive && (!activeSessionId.HasValue || x.AcademicSessionId == activeSessionId.Value), cancellationToken);
            var teacherClassIds = await _db.TeacherAssignments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.TeacherUserId == user.Id && x.IsActive && (!activeSessionId.HasValue || x.AcademicSessionId == activeSessionId.Value))
                .Select(x => x.SchoolClassId).Distinct().ToListAsync(cancellationToken);
            model.AssignedStudents = await _db.StudentEnrollments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.IsCurrent && x.Status == StudentEnrollmentStatus.Active && x.SchoolClassId.HasValue && teacherClassIds.Contains(x.SchoolClassId.Value))
                .Select(x => x.StudentId).Distinct().CountAsync(cancellationToken);
        }

        if (model.ShowAudit)
        {
            model.RecentActivity = await _db.AuditLogs.AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(7)
                .Select(x => new DashboardAuditItem
                {
                    Action = x.Action,
                    EntityType = x.EntityType,
                    UserName = x.UserEmail ?? "System",
                    Date = x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);
        }

        BuildAlerts(model);
        return model;
    }

    private static void BuildAlerts(DashboardViewModel model)
    {
        if (!model.HasSchoolContext) return;

        if (model.ShowAttendance && model.UnmarkedSections > 0)
        {
            model.Alerts.Add(new DashboardAlertItem
            {
                Title = "Attendance pending",
                Message = $"{model.UnmarkedSections} section(s) have not submitted attendance today.",
                Tone = "warning",
                Controller = "Attendance",
                Action = "Index"
            });
        }
        if (model.ShowFinance && model.FeeDefaulters > 0)
        {
            model.Alerts.Add(new DashboardAlertItem
            {
                Title = "Fee follow-up",
                Message = $"{model.FeeDefaulters} student(s) currently have overdue fee balances.",
                Tone = "danger",
                Controller = "Fees",
                Action = "Defaulters"
            });
        }
        if (model.ShowFinance && model.PendingExpenseApprovals > 0)
        {
            model.Alerts.Add(new DashboardAlertItem
            {
                Title = "Expense approvals",
                Message = $"{model.PendingExpenseApprovals} expense(s) are waiting for approval.",
                Tone = "info",
                Controller = "Expenses",
                Action = "Index"
            });
        }
        if (model.ShowAdmissions && model.PendingAdmissions > 0)
        {
            model.Alerts.Add(new DashboardAlertItem
            {
                Title = "Admission applications",
                Message = $"{model.PendingAdmissions} application(s) need review or completion.",
                Tone = "info",
                Controller = "Admissions",
                Action = "Applications"
            });
        }
        if (model.ShowAttendance && model.LowAttendanceStudents > 0)
        {
            model.Alerts.Add(new DashboardAlertItem
            {
                Title = "Low attendance",
                Message = $"{model.LowAttendanceStudents} student(s) are below 75% this month.",
                Tone = "warning",
                Controller = "Attendance",
                Action = "MonthlyReport"
            });
        }
        if (model.Alerts.Count == 0)
        {
            model.Alerts.Add(new DashboardAlertItem
            {
                Title = "All clear",
                Message = "No priority operational alerts are currently detected.",
                Tone = "success"
            });
        }
    }

    private static string GetRoleDisplayName(string role) => role switch
    {
        AppRoles.SuperAdmin => "Super Admin / Vendor",
        AppRoles.Principal => "School Owner / Principal",
        AppRoles.Admin => "Administrator",
        AppRoles.Accountant => "Accountant",
        AppRoles.Teacher => "Teacher / Class Teacher",
        AppRoles.ExamController => "Exam Controller",
        AppRoles.HR => "HR / Payroll Officer",
        AppRoles.Receptionist => "Reception / Admission Officer",
        AppRoles.Parent => "Parent",
        AppRoles.Student => "Student",
        _ => "User"
    };
}
