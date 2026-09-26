using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace School_Management_System.ViewModels;

public class SettingsPageViewModel
{
    public int Id { get; set; }

    [Display(Name = "Admission prefix"), Required, StringLength(12)] public string AdmissionNumberPrefix { get; set; } = "STD";
    [Display(Name = "Admission sequence digits"), Range(3, 8)] public int AdmissionNumberDigits { get; set; } = 4;
    [Display(Name = "Challan prefix"), Required, StringLength(12)] public string ChallanNumberPrefix { get; set; } = "CH";
    [Display(Name = "Receipt prefix"), Required, StringLength(12)] public string ReceiptNumberPrefix { get; set; } = "RC";
    [Display(Name = "Financial sequence digits"), Range(4, 10)] public int FinancialNumberDigits { get; set; } = 6;
    [Display(Name = "Fiscal year starts in"), Range(1, 12)] public int FiscalYearStartMonth { get; set; } = 7;

    [Display(Name = "Default fee due day"), Range(1, 28)] public int DefaultFeeDueDay { get; set; } = 10;
    [Display(Name = "Fixed late fee"), Range(0, 1000000)] public decimal LateFeeFixedAmount { get; set; }
    [Display(Name = "Late fee grace days"), Range(0, 60)] public int LateFeeGraceDays { get; set; }
    [Display(Name = "Apply late fee automatically")] public bool ApplyLateFeeOnCollection { get; set; } = true;
    [Display(Name = "Auto-generate monthly challans")] public bool AutoGenerateMonthlyChallans { get; set; }
    [Display(Name = "Monthly generation day"), Range(1, 28)] public int MonthlyChallanGenerationDay { get; set; } = 1;

    [Display(Name = "Teacher edit cutoff (hours after day end)"), Range(0, 168)] public int TeacherAttendanceEditCutoffHours { get; set; } = 24;
    [Display(Name = "Low attendance threshold (%)"), Range(0, 100)] public decimal LowAttendanceThresholdPercent { get; set; } = 75m;
    [Display(Name = "Allow Late status")] public bool AttendanceAllowLate { get; set; } = true;
    [Display(Name = "Allow Leave status")] public bool AttendanceAllowLeave { get; set; } = true;
    [Display(Name = "Allow Half Day status")] public bool AttendanceAllowHalfDay { get; set; } = true;
    [Display(Name = "Allow No Class status")] public bool AttendanceAllowNoClass { get; set; } = true;

    [Display(Name = "Show attendance on result card")] public bool ResultCardShowAttendance { get; set; } = true;
    [Display(Name = "Show class position on result card")] public bool ResultCardShowClassPosition { get; set; } = true;
    [Display(Name = "Class teacher signature label"), StringLength(120)] public string ClassTeacherSignatureLabel { get; set; } = "Class Teacher Signature";
    [Display(Name = "Principal signature label"), StringLength(120)] public string PrincipalSignatureLabel { get; set; } = "Principal Signature";
    [Display(Name = "Result card footer"), StringLength(500)] public string? ResultCardFooterText { get; set; }
    [Display(Name = "Payslip footer"), StringLength(500)] public string? PayslipFooterText { get; set; }
    [Display(Name = "General print footer"), StringLength(500)] public string? GeneralPrintFooterText { get; set; }
    public string? PrincipalSignaturePath { get; set; }
    public string? ClassTeacherSignaturePath { get; set; }
    [Display(Name = "Principal signature image")] public IFormFile? PrincipalSignatureFile { get; set; }
    [Display(Name = "Class teacher signature image")] public IFormFile? ClassTeacherSignatureFile { get; set; }

    [Display(Name = "Scheduled backups")] public bool ScheduledBackupsEnabled { get; set; } = true;
    [Display(Name = "Backup hour (local time)"), Range(0, 23)] public int BackupHourLocal { get; set; } = 2;
    [Display(Name = "Backup retention days"), Range(1, 3650)] public int BackupRetentionDays { get; set; } = 30;
    [Display(Name = "Allow in-app restore")] public bool AllowInAppRestore { get; set; } = true;

    [Display(Name = "Session timeout (minutes)"), Range(5, 720)] public int SessionTimeoutMinutes { get; set; } = 30;
    [Display(Name = "Minimum password length"), Range(6, 32)] public int PasswordRequiredLength { get; set; } = 8;
    [Display(Name = "Require a digit")] public bool PasswordRequireDigit { get; set; } = true;
    [Display(Name = "Require uppercase")] public bool PasswordRequireUppercase { get; set; } = true;
    [Display(Name = "Require lowercase")] public bool PasswordRequireLowercase { get; set; } = true;
    [Display(Name = "Require special character")] public bool PasswordRequireSpecialCharacter { get; set; }

    [Display(Name = "Enable email notifications")] public bool EmailNotificationsEnabled { get; set; }
    [Display(Name = "Enable SMS notifications")] public bool SmsNotificationsEnabled { get; set; }
    [Display(Name = "Enable WhatsApp notifications")] public bool WhatsAppNotificationsEnabled { get; set; }
    [Display(Name = "Email provider"), StringLength(120)] public string? EmailProviderName { get; set; }
    [Display(Name = "SMS provider"), StringLength(120)] public string? SmsProviderName { get; set; }
    [Display(Name = "WhatsApp provider"), StringLength(120)] public string? WhatsAppProviderName { get; set; }

    [Display(Name = "Biometric attendance")] public bool EnableBiometricAttendance { get; set; } = true;
    [Display(Name = "Camera attendance")] public bool EnableCameraAttendance { get; set; } = true;
    [Display(Name = "Parent / student portal")] public bool EnableParentStudentPortal { get; set; }
    [Display(Name = "Online payments")] public bool EnableOnlinePayments { get; set; }
    [Display(Name = "Push notifications")] public bool EnablePushNotifications { get; set; }

    [Display(Name = "School time zone"), Required, StringLength(80)] public string SchoolTimeZoneId { get; set; } = "Asia/Karachi";

    public string SchoolName { get; set; } = string.Empty;
    public string? SchoolLogoPath { get; set; }
    public string? ActiveSessionName { get; set; }
    public int FeeHeadCount { get; set; }
    public int GradingRuleCount { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
}
