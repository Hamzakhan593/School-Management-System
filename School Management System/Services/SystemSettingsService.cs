using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly ApplicationDbContext _db;

    public SystemSettingsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SystemSetting> GetAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId, cancellationToken);
        return existing ?? NewDefaults(schoolId);
    }

    public async Task<SystemSetting> GetTrackedAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SystemSettings
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId, cancellationToken);
        if (existing is not null) return existing;

        var created = NewDefaults(schoolId);
        _db.SystemSettings.Add(created);
        return created;
    }

    private static SystemSetting NewDefaults(int schoolId) => new()
    {
        SchoolId = schoolId,
        AdmissionNumberPrefix = "STD",
        AdmissionNumberDigits = 4,
        ChallanNumberPrefix = "CH",
        ReceiptNumberPrefix = "RC",
        FinancialNumberDigits = 6,
        FiscalYearStartMonth = 7,
        DefaultFeeDueDay = 10,
        LateFeeFixedAmount = 0,
        LateFeeGraceDays = 0,
        ApplyLateFeeOnCollection = true,
        AutoGenerateMonthlyChallans = false,
        MonthlyChallanGenerationDay = 1,
        TeacherAttendanceEditCutoffHours = 24,
        LowAttendanceThresholdPercent = 75m,
        AttendanceAllowLate = true,
        AttendanceAllowLeave = true,
        AttendanceAllowHalfDay = true,
        AttendanceAllowNoClass = true,
        ResultCardShowAttendance = true,
        ResultCardShowClassPosition = true,
        ClassTeacherSignatureLabel = "Class Teacher Signature",
        PrincipalSignatureLabel = "Principal Signature",
        ScheduledBackupsEnabled = true,
        BackupHourLocal = 2,
        BackupRetentionDays = 30,
        AllowInAppRestore = true,
        SessionTimeoutMinutes = 30,
        PasswordRequiredLength = 8,
        PasswordRequireDigit = true,
        PasswordRequireUppercase = true,
        PasswordRequireLowercase = true,
        PasswordRequireSpecialCharacter = false,
        EnableBiometricAttendance = true,
        EnableCameraAttendance = true,
        SchoolTimeZoneId = "Asia/Karachi"
    };
}
