namespace School_Management_System.Options;

public class BackupOptions
{
    public string RootPath { get; set; } = "App_Data/Backups";
    public bool ScheduledBackupsEnabled { get; set; } = true;
    public int ScheduledHourLocal { get; set; } = 2;
    public int RetentionDays { get; set; } = 30;
    public string TimeZoneId { get; set; } = "Asia/Karachi";
    public bool AllowInAppRestore { get; set; } = true;
}
