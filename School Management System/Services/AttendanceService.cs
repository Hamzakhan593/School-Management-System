using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Options;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _db;
    private readonly AttendanceOptions _options;
    private readonly ISystemSettingsService _settings;

    public AttendanceService(ApplicationDbContext db, IOptions<AttendanceOptions> options, ISystemSettingsService settings)
    {
        _db = db;
        _options = options.Value;
        _settings = settings;
    }

    public async Task<AttendanceMarkingViewModel> BuildMarkingSheetAsync(
        int schoolId,
        string currentUserId,
        bool isManager,
        int? academicSessionId,
        int? schoolClassId,
        int? sectionId,
        DateTime attendanceDate,
        CancellationToken cancellationToken = default)
    {
        var day = attendanceDate.Date;
        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status != AcademicSessionStatus.Archived)
            .OrderByDescending(x => x.Status == AcademicSessionStatus.Active)
            .ThenByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

        var selectedSession = academicSessionId.HasValue
            ? sessions.FirstOrDefault(x => x.Id == academicSessionId.Value)
            : sessions.FirstOrDefault(x => x.Status == AcademicSessionStatus.Active) ?? sessions.FirstOrDefault();

        var attendanceSettings = await _settings.GetAsync(schoolId, cancellationToken);
        var model = new AttendanceMarkingViewModel
        {
            AcademicSessionId = selectedSession?.Id ?? 0,
            SchoolClassId = schoolClassId ?? 0,
            SectionId = sectionId,
            AttendanceDate = day,
            Sessions = sessions,
            AllowedStatuses = GetAllowedStatuses(attendanceSettings),
            IsManager = isManager
        };

        if (selectedSession is null)
        {
            model.AccessMessage = "Create an academic session before marking attendance.";
            return model;
        }

        model.SelectedSessionName = selectedSession.Name;
        if (day < selectedSession.StartDate.Date || day > selectedSession.EndDate.Date)
        {
            model.AccessMessage = $"Attendance date must be inside academic session {selectedSession.Name}.";
        }

        var classQuery = _db.SchoolClasses.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive);

        if (!isManager)
        {
            var assignedClassIds = await GetTeacherClassIdsAsync(schoolId, selectedSession.Id, currentUserId, cancellationToken);
            classQuery = classQuery.Where(x => assignedClassIds.Contains(x.Id));
        }

        model.Classes = await classQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (!schoolClassId.HasValue || schoolClassId.Value <= 0)
        {
            model.AccessMessage ??= "Select a class and attendance date to load the roster.";
            return model;
        }

        var selectedClass = model.Classes.FirstOrDefault(x => x.Id == schoolClassId.Value);
        if (selectedClass is null)
        {
            model.AccessMessage = isManager
                ? "Selected class is not available."
                : "You are not assigned to the selected class.";
            return model;
        }

        model.SchoolClassId = selectedClass.Id;
        model.SelectedClassName = selectedClass.Name;

        var allSections = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.SchoolClassId == selectedClass.Id && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (isManager)
        {
            model.Sections = allSections;
        }
        else
        {
            var canUseAllSections = await HasClassWideTeacherAccessAsync(
                schoolId, selectedSession.Id, selectedClass.Id, currentUserId, cancellationToken);

            if (canUseAllSections)
            {
                model.Sections = allSections;
            }
            else
            {
                var allowedSectionIds = await GetTeacherSectionIdsAsync(
                    schoolId, selectedSession.Id, selectedClass.Id, currentUserId, cancellationToken);
                model.Sections = allSections.Where(x => allowedSectionIds.Contains(x.Id)).ToList();
            }
        }

        if (allSections.Count > 0 && !sectionId.HasValue)
        {
            model.AccessMessage ??= "Select a section to mark attendance.";
            return model;
        }

        if (sectionId.HasValue)
        {
            var selectedSection = model.Sections.FirstOrDefault(x => x.Id == sectionId.Value);
            if (selectedSection is null)
            {
                model.AccessMessage = isManager
                    ? "Selected section does not belong to this class."
                    : "You are not assigned to the selected section.";
                return model;
            }
            model.SelectedSectionName = selectedSection.Name;
        }
        else if (!isManager)
        {
            var hasClassWideAccess = await HasClassWideTeacherAccessAsync(
                schoolId, selectedSession.Id, selectedClass.Id, currentUserId, cancellationToken);
            if (!hasClassWideAccess)
            {
                model.AccessMessage = "You are not assigned to mark this class.";
                return model;
            }
        }

        if (model.AccessMessage is not null && (day < selectedSession.StartDate.Date || day > selectedSession.EndDate.Date))
        {
            return model;
        }

        var enrollmentsQuery = _db.StudentEnrollments.AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == selectedSession.Id
                && x.SchoolClassId == selectedClass.Id
                && x.EffectiveFrom <= day
                && (x.EffectiveTo == null || x.EffectiveTo >= day));

        if (sectionId.HasValue)
            enrollmentsQuery = enrollmentsQuery.Where(x => x.SectionId == sectionId.Value);
        else
            enrollmentsQuery = enrollmentsQuery.Where(x => x.SectionId == null);

        var enrollments = await enrollmentsQuery
            .OrderBy(x => x.RollNumber)
            .ThenBy(x => x.Student.FullName)
            .ToListAsync(cancellationToken);

        var studentIds = enrollments.Select(x => x.StudentId).ToArray();
        var existing = studentIds.Length == 0
            ? new Dictionary<int, StudentAttendance>()
            : await _db.StudentAttendances.AsNoTracking()
                .Where(x => x.SchoolId == schoolId
                    && x.AcademicSessionId == selectedSession.Id
                    && x.AttendanceDate == day
                    && studentIds.Contains(x.StudentId))
                .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        model.Students = enrollments.Select(enrollment =>
        {
            existing.TryGetValue(enrollment.StudentId, out var attendance);
            return new AttendanceStudentRowViewModel
            {
                StudentId = enrollment.StudentId,
                StudentEnrollmentId = enrollment.Id,
                AttendanceId = attendance?.Id,
                AdmissionNumber = enrollment.Student.AdmissionNumber,
                RollNumber = enrollment.RollNumber ?? enrollment.Student.RollNumber,
                StudentName = enrollment.Student.FullName,
                Status = attendance?.Status ?? (StudentAttendanceStatus)0,
                Source = attendance?.Source ?? AttendanceSource.Manual,
                Remarks = attendance?.Remarks,
                AlreadySaved = attendance is not null
            };
        }).ToList();

        var afterCutoff = IsAfterTeacherCutoff(day, attendanceSettings.TeacherAttendanceEditCutoffHours);
        model.CanEdit = isManager || !afterCutoff;
        model.RequiresCorrectionReason = isManager && afterCutoff;

        if (!model.CanEdit)
            model.AccessMessage = "Teacher edit cutoff has passed. A Principal/Admin must make any later correction.";

        return model;
    }

    public async Task<AttendanceSaveResult> SaveClassAttendanceAsync(
        int schoolId,
        string currentUserId,
        bool isManager,
        AttendanceMarkingViewModel model,
        CancellationToken cancellationToken = default)
    {
        var day = model.AttendanceDate.Date;
        var attendanceSettings = await _settings.GetAsync(schoolId, cancellationToken);

        var session = await _db.AcademicSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.AcademicSessionId && x.SchoolId == schoolId, cancellationToken);
        if (session is null)
            return new(false, "Academic session was not found.");
        if (day < session.StartDate.Date || day > session.EndDate.Date)
            return new(false, $"Attendance date must be inside academic session {session.Name}.");

        var schoolClass = await _db.SchoolClasses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.SchoolClassId && x.SchoolId == schoolId && x.IsActive, cancellationToken);
        if (schoolClass is null)
            return new(false, "Class was not found.");

        var sectionsExist = await _db.Sections.AsNoTracking()
            .AnyAsync(x => x.SchoolId == schoolId && x.SchoolClassId == schoolClass.Id && x.IsActive, cancellationToken);

        if (sectionsExist && !model.SectionId.HasValue)
            return new(false, "Select a section before saving attendance.");

        if (model.SectionId.HasValue)
        {
            var validSection = await _db.Sections.AsNoTracking().AnyAsync(x =>
                x.Id == model.SectionId.Value && x.SchoolId == schoolId && x.SchoolClassId == schoolClass.Id && x.IsActive,
                cancellationToken);
            if (!validSection) return new(false, "Selected section is invalid.");
        }

        if (!isManager)
        {
            var hasAccess = model.SectionId.HasValue
                ? await HasTeacherSectionAccessAsync(schoolId, session.Id, schoolClass.Id, model.SectionId.Value, currentUserId, cancellationToken)
                : await HasClassWideTeacherAccessAsync(schoolId, session.Id, schoolClass.Id, currentUserId, cancellationToken);

            if (!hasAccess)
                return new(false, "You are not assigned to mark attendance for this class/section.");
        }

        var afterCutoff = IsAfterTeacherCutoff(day, attendanceSettings.TeacherAttendanceEditCutoffHours);
        if (afterCutoff && !isManager)
            return new(false, "Teacher edit cutoff has passed. Ask a Principal/Admin to make the correction.");
        if (afterCutoff && isManager && string.IsNullOrWhiteSpace(model.CorrectionReason))
            return new(false, "A correction reason is required after the attendance edit cutoff.");

        var rosterQuery = _db.StudentEnrollments
            .Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == session.Id
                && x.SchoolClassId == schoolClass.Id
                && x.EffectiveFrom <= day
                && (x.EffectiveTo == null || x.EffectiveTo >= day));

        rosterQuery = model.SectionId.HasValue
            ? rosterQuery.Where(x => x.SectionId == model.SectionId.Value)
            : rosterQuery.Where(x => x.SectionId == null);

        var roster = await rosterQuery.ToListAsync(cancellationToken);
        if (roster.Count == 0)
            return new(false, "No students are enrolled in this class/section for the selected date.");

        var postedRows = model.Students ?? [];
        if (postedRows.Any(x => (int)x.Status == 0))
            return new(false, "Some students are unmarked. Choose a status for every student before saving.");
        if (postedRows.Count != roster.Count)
            return new(false, "The attendance roster changed. Reload the page and try again.");

        var rosterByStudent = roster.ToDictionary(x => x.StudentId);
        if (postedRows.Select(x => x.StudentId).Distinct().Count() != postedRows.Count
            || postedRows.Any(x => !rosterByStudent.ContainsKey(x.StudentId)))
            return new(false, "Attendance submission contains an invalid student. Reload the page.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var studentIds = roster.Select(x => x.StudentId).ToArray();
        var existing = await _db.StudentAttendances
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == session.Id
                && x.AttendanceDate == day
                && studentIds.Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        var created = 0;
        var updated = 0;
        var allowedStatuses = GetAllowedStatuses(attendanceSettings).ToHashSet();

        foreach (var row in postedRows)
        {
            if (!Enum.IsDefined(typeof(StudentAttendanceStatus), row.Status) || !allowedStatuses.Contains(row.Status))
                return new(false, $"Attendance status {row.Status} is disabled in Settings for student {row.StudentName}.");

            var enrollment = rosterByStudent[row.StudentId];
            var cleanRemarks = NullIfBlank(row.Remarks);

            if (existing.TryGetValue(row.StudentId, out var attendance))
            {
                attendance.Status = row.Status;
                attendance.Remarks = cleanRemarks;
                attendance.StudentEnrollmentId = enrollment.Id;
                attendance.SchoolClassId = schoolClass.Id;
                attendance.SectionId = model.SectionId;
                attendance.LastModifiedByUserId = currentUserId;
                attendance.LastModifiedAtUtc = DateTime.UtcNow;
                // Preserve original Source so future biometric/camera attendance remains traceable.
                updated++;
            }
            else
            {
                _db.StudentAttendances.Add(new StudentAttendance
                {
                    SchoolId = schoolId,
                    AcademicSessionId = session.Id,
                    StudentId = row.StudentId,
                    StudentEnrollmentId = enrollment.Id,
                    SchoolClassId = schoolClass.Id,
                    SectionId = model.SectionId,
                    AttendanceDate = day,
                    Status = row.Status,
                    Source = AttendanceSource.Manual,
                    Remarks = cleanRemarks,
                    MarkedByUserId = currentUserId,
                    MarkedAtUtc = DateTime.UtcNow
                });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(true, $"Attendance saved for {postedRows.Count} students.", created, updated, afterCutoff);
    }

    public async Task<MonthlyAttendanceReportViewModel> BuildMonthlyReportAsync(
        int schoolId,
        string currentUserId,
        bool isManager,
        int? academicSessionId,
        int? schoolClassId,
        int? sectionId,
        DateTime month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var attendanceSettings = await _settings.GetAsync(schoolId, cancellationToken);

        var sessions = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.Status != AcademicSessionStatus.Archived)
            .OrderByDescending(x => x.Status == AcademicSessionStatus.Active)
            .ThenByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

        var selectedSession = academicSessionId.HasValue
            ? sessions.FirstOrDefault(x => x.Id == academicSessionId.Value)
            : sessions.FirstOrDefault(x => x.Status == AcademicSessionStatus.Active) ?? sessions.FirstOrDefault();

        var model = new MonthlyAttendanceReportViewModel
        {
            AcademicSessionId = selectedSession?.Id,
            SchoolClassId = schoolClassId,
            SectionId = sectionId,
            Month = monthStart,
            Sessions = sessions,
            LowAttendanceThresholdPercent = attendanceSettings.LowAttendanceThresholdPercent
        };

        if (selectedSession is null)
        {
            model.AccessMessage = "Create an academic session before opening attendance reports.";
            return model;
        }
        model.SelectedSessionName = selectedSession.Name;

        var classQuery = _db.SchoolClasses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive);
        if (!isManager)
        {
            var assignedClassIds = await GetTeacherClassIdsAsync(schoolId, selectedSession.Id, currentUserId, cancellationToken);
            classQuery = classQuery.Where(x => assignedClassIds.Contains(x.Id));
        }
        model.Classes = await classQuery.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);

        if (!schoolClassId.HasValue)
        {
            model.AccessMessage = "Select a class to view the monthly report.";
            return model;
        }

        var selectedClass = model.Classes.FirstOrDefault(x => x.Id == schoolClassId.Value);
        if (selectedClass is null)
        {
            model.AccessMessage = isManager ? "Selected class is not available." : "You are not assigned to the selected class.";
            return model;
        }
        model.SelectedClassName = selectedClass.Name;

        var allSections = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.SchoolClassId == selectedClass.Id && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (isManager)
        {
            model.Sections = allSections;
        }
        else
        {
            var classWide = await HasClassWideTeacherAccessAsync(schoolId, selectedSession.Id, selectedClass.Id, currentUserId, cancellationToken);
            if (classWide)
                model.Sections = allSections;
            else
            {
                var allowedSectionIds = await GetTeacherSectionIdsAsync(schoolId, selectedSession.Id, selectedClass.Id, currentUserId, cancellationToken);
                model.Sections = allSections.Where(x => allowedSectionIds.Contains(x.Id)).ToList();
            }
        }

        if (!isManager && allSections.Count > 0 && !sectionId.HasValue)
        {
            model.AccessMessage = "Select one of your assigned sections to view attendance.";
            return model;
        }

        if (sectionId.HasValue)
        {
            var selectedSection = model.Sections.FirstOrDefault(x => x.Id == sectionId.Value);
            if (selectedSection is null)
            {
                model.AccessMessage = isManager ? "Selected section is invalid." : "You are not assigned to this section.";
                return model;
            }
            model.SelectedSectionName = selectedSection.Name;
        }

        var overlapStart = monthStart < selectedSession.StartDate.Date ? selectedSession.StartDate.Date : monthStart;
        var overlapEnd = monthEnd > selectedSession.EndDate.Date ? selectedSession.EndDate.Date : monthEnd;
        if (overlapStart > overlapEnd)
        {
            model.AccessMessage = "The selected month is outside this academic session.";
            return model;
        }

        var enrollmentQuery = _db.StudentEnrollments.AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == selectedSession.Id
                && x.SchoolClassId == selectedClass.Id
                && x.EffectiveFrom <= overlapEnd
                && (x.EffectiveTo == null || x.EffectiveTo >= overlapStart));

        if (sectionId.HasValue)
            enrollmentQuery = enrollmentQuery.Where(x => x.SectionId == sectionId.Value);

        var enrollments = await enrollmentQuery
            .OrderBy(x => x.Student.FullName)
            .ToListAsync(cancellationToken);

        var studentIds = enrollments.Select(x => x.StudentId).Distinct().ToArray();
        List<StudentAttendance> records;
        if (studentIds.Length == 0)
        {
            records = [];
        }
        else
        {
            records = await _db.StudentAttendances.AsNoTracking()
                .Where(x => x.SchoolId == schoolId
                    && x.AcademicSessionId == selectedSession.Id
                    && x.SchoolClassId == selectedClass.Id
                    && x.AttendanceDate >= overlapStart
                    && x.AttendanceDate <= overlapEnd
                    && studentIds.Contains(x.StudentId)
                    && (!sectionId.HasValue || x.SectionId == sectionId.Value))
                .ToListAsync(cancellationToken);
        }

        var enrollmentByStudent = enrollments
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EffectiveFrom).First());

        model.Students = enrollmentByStudent.Values.Select(enrollment =>
        {
            var studentRecords = records.Where(x => x.StudentId == enrollment.StudentId).ToList();
            var countable = studentRecords.Where(x => x.Status is not StudentAttendanceStatus.Holiday and not StudentAttendanceStatus.NoClass).ToList();
            var present = countable.Count(x => x.Status == StudentAttendanceStatus.Present);
            var late = countable.Count(x => x.Status == StudentAttendanceStatus.Late);
            var half = countable.Count(x => x.Status == StudentAttendanceStatus.HalfDay);
            var attendedEquivalent = present + late + (half * 0.5m);
            var percentage = countable.Count == 0 ? 0m : Math.Round(attendedEquivalent * 100m / countable.Count, 2);

            return new MonthlyAttendanceStudentRowViewModel
            {
                StudentId = enrollment.StudentId,
                AdmissionNumber = enrollment.Student.AdmissionNumber,
                RollNumber = enrollment.RollNumber ?? enrollment.Student.RollNumber,
                StudentName = enrollment.Student.FullName,
                MarkedDays = countable.Count,
                PresentDays = present,
                AbsentDays = countable.Count(x => x.Status == StudentAttendanceStatus.Absent),
                LateDays = late,
                LeaveDays = countable.Count(x => x.Status == StudentAttendanceStatus.Leave),
                HalfDays = half,
                AttendancePercentage = percentage,
                IsLowAttendance = countable.Count > 0 && percentage < attendanceSettings.LowAttendanceThresholdPercent
            };
        })
        .OrderBy(x => x.RollNumber)
        .ThenBy(x => x.StudentName)
        .ToList();

        return model;
    }

    private static bool IsAfterTeacherCutoff(DateTime attendanceDay, int cutoffHours)
    {
        var cutoff = attendanceDay.Date.AddDays(1).AddHours(Math.Max(0, cutoffHours));
        return DateTime.Now > cutoff;
    }

    private static List<StudentAttendanceStatus> GetAllowedStatuses(SystemSetting settings)
    {
        var statuses = new List<StudentAttendanceStatus>
        {
            StudentAttendanceStatus.Present,
            StudentAttendanceStatus.Absent,
            StudentAttendanceStatus.Holiday
        };
        if (settings.AttendanceAllowLate) statuses.Add(StudentAttendanceStatus.Late);
        if (settings.AttendanceAllowLeave) statuses.Add(StudentAttendanceStatus.Leave);
        if (settings.AttendanceAllowHalfDay) statuses.Add(StudentAttendanceStatus.HalfDay);
        if (settings.AttendanceAllowNoClass) statuses.Add(StudentAttendanceStatus.NoClass);
        return statuses.OrderBy(x => (int)x).ToList();
    }

    private async Task<HashSet<int>> GetTeacherClassIdsAsync(
        int schoolId,
        int academicSessionId,
        string userId,
        CancellationToken cancellationToken)
    {
        var assignmentClassIds = await _db.TeacherAssignments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == academicSessionId && x.TeacherUserId == userId && x.IsActive)
            .Select(x => x.SchoolClassId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var classTeacherClassIds = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.ClassTeacherUserId == userId && x.IsActive)
            .Select(x => x.SchoolClassId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return assignmentClassIds.Concat(classTeacherClassIds).ToHashSet();
    }

    private async Task<HashSet<int>> GetTeacherSectionIdsAsync(
        int schoolId,
        int academicSessionId,
        int schoolClassId,
        string userId,
        CancellationToken cancellationToken)
    {
        var assignmentSectionIds = await _db.TeacherAssignments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId
                && x.AcademicSessionId == academicSessionId
                && x.SchoolClassId == schoolClassId
                && x.TeacherUserId == userId
                && x.IsActive
                && x.SectionId != null)
            .Select(x => x.SectionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var classTeacherSectionIds = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.SchoolClassId == schoolClassId && x.ClassTeacherUserId == userId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return assignmentSectionIds.Concat(classTeacherSectionIds).ToHashSet();
    }

    private Task<bool> HasClassWideTeacherAccessAsync(
        int schoolId,
        int academicSessionId,
        int schoolClassId,
        string userId,
        CancellationToken cancellationToken)
        => _db.TeacherAssignments.AsNoTracking().AnyAsync(x =>
            x.SchoolId == schoolId
            && x.AcademicSessionId == academicSessionId
            && x.SchoolClassId == schoolClassId
            && x.TeacherUserId == userId
            && x.IsActive
            && x.SectionId == null,
            cancellationToken);

    private async Task<bool> HasTeacherSectionAccessAsync(
        int schoolId,
        int academicSessionId,
        int schoolClassId,
        int sectionId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (await HasClassWideTeacherAccessAsync(schoolId, academicSessionId, schoolClassId, userId, cancellationToken))
            return true;

        var isClassTeacher = await _db.Sections.AsNoTracking().AnyAsync(x =>
            x.Id == sectionId && x.SchoolId == schoolId && x.SchoolClassId == schoolClassId && x.ClassTeacherUserId == userId && x.IsActive,
            cancellationToken);
        if (isClassTeacher) return true;

        return await _db.TeacherAssignments.AsNoTracking().AnyAsync(x =>
            x.SchoolId == schoolId
            && x.AcademicSessionId == academicSessionId
            && x.SchoolClassId == schoolClassId
            && x.SectionId == sectionId
            && x.TeacherUserId == userId
            && x.IsActive,
            cancellationToken);
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
