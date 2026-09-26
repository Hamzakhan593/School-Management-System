using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public class SystemSetting
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, StringLength(12)]
    public string AdmissionNumberPrefix { get; set; } = "STD";
    [Range(3, 8)] public int AdmissionNumberDigits { get; set; } = 4;

    [Required, StringLength(12)]
    public string ChallanNumberPrefix { get; set; } = "CH";
    [Required, StringLength(12)]
    public string ReceiptNumberPrefix { get; set; } = "RC";
    [Range(4, 10)] public int FinancialNumberDigits { get; set; } = 6;
    [Range(1, 12)] public int FiscalYearStartMonth { get; set; } = 7;

    [Range(1, 28)] public int DefaultFeeDueDay { get; set; } = 10;
    [Range(0, 1000000)] public decimal LateFeeFixedAmount { get; set; }
    [Range(0, 60)] public int LateFeeGraceDays { get; set; }
    public bool ApplyLateFeeOnCollection { get; set; } = true;
    public bool AutoGenerateMonthlyChallans { get; set; }
    [Range(1, 28)] public int MonthlyChallanGenerationDay { get; set; } = 1;

    [Range(0, 168)] public int TeacherAttendanceEditCutoffHours { get; set; } = 24;
    [Range(0, 100)] public decimal LowAttendanceThresholdPercent { get; set; } = 75m;
    public bool AttendanceAllowLate { get; set; } = true;
    public bool AttendanceAllowLeave { get; set; } = true;
    public bool AttendanceAllowHalfDay { get; set; } = true;
    public bool AttendanceAllowNoClass { get; set; } = true;

    public bool ResultCardShowAttendance { get; set; } = true;
    public bool ResultCardShowClassPosition { get; set; } = true;
    [StringLength(120)] public string ClassTeacherSignatureLabel { get; set; } = "Class Teacher Signature";
    [StringLength(120)] public string PrincipalSignatureLabel { get; set; } = "Principal Signature";
    [StringLength(500)] public string? ResultCardFooterText { get; set; }
    [StringLength(500)] public string? PayslipFooterText { get; set; }
    [StringLength(500)] public string? GeneralPrintFooterText { get; set; }
    [StringLength(500)] public string? PrincipalSignaturePath { get; set; }
    [StringLength(500)] public string? ClassTeacherSignaturePath { get; set; }

    public bool ScheduledBackupsEnabled { get; set; } = true;
    [Range(0, 23)] public int BackupHourLocal { get; set; } = 2;
    [Range(1, 3650)] public int BackupRetentionDays { get; set; } = 30;
    public bool AllowInAppRestore { get; set; } = true;

    [Range(5, 720)] public int SessionTimeoutMinutes { get; set; } = 30;
    [Range(6, 32)] public int PasswordRequiredLength { get; set; } = 8;
    public bool PasswordRequireDigit { get; set; } = true;
    public bool PasswordRequireUppercase { get; set; } = true;
    public bool PasswordRequireLowercase { get; set; } = true;
    public bool PasswordRequireSpecialCharacter { get; set; }

    public bool EmailNotificationsEnabled { get; set; }
    public bool SmsNotificationsEnabled { get; set; }
    public bool WhatsAppNotificationsEnabled { get; set; }
    [StringLength(120)] public string? EmailProviderName { get; set; }
    [StringLength(120)] public string? SmsProviderName { get; set; }
    [StringLength(120)] public string? WhatsAppProviderName { get; set; }

    public bool EnableBiometricAttendance { get; set; } = true;
    public bool EnableCameraAttendance { get; set; } = true;
    public bool EnableParentStudentPortal { get; set; }
    public bool EnableOnlinePayments { get; set; }
    public bool EnablePushNotifications { get; set; }

    [Required, StringLength(80)] public string SchoolTimeZoneId { get; set; } = "Asia/Karachi";

    [StringLength(450)] public string? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public School School { get; set; } = null!;
    public ApplicationUser? UpdatedByUser { get; set; }
}
