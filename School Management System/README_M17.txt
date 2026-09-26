M17 — BACKUP, RESTORE & AUDIT LOGS
School Management Software — ASP.NET Core MVC 9 / EF Core 9 / SQL Server
=======================================================================

WHAT THIS PATCH ADDS
--------------------
1. Backup & Recovery Center for Principal/Admin/SuperAdmin.
2. Manual full SQL Server .bak backups.
3. Automatic daily scheduled backup hosted service.
4. Backup history with type, status, size, SHA-256, date, user and storage reference.
5. RESTORE VERIFYONLY + SHA-256 integrity verification.
6. Configurable retention with automatic cleanup of expired backup files while history remains.
7. Controlled restore workflow for Principal/SuperAdmin only:
   - selected backup is verified first;
   - user must re-enter the current password;
   - user must type RESTORE exactly;
   - a written reason is required;
   - a fresh safety backup is created automatically before restore;
   - restore history is written after recovery.
8. Expanded audit log schema: SchoolId, OldValues and NewValues in addition to user, action,
   entity, timestamp, IP and user-agent/device context.
9. Searchable M17 Audit Logs screen.
10. M14 Reports Center now links to live Backup & Recovery and the new Audit Logs.
11. Navigation adds Administration -> Backup & Audit.

IMPORTANT: This module uses SQL Server native BACKUP DATABASE / RESTORE DATABASE commands.
It does NOT fake backups by exporting a few tables or copying the MDF while the database is live.

INSTALLATION
------------
Extract this ZIP INSIDE the inner project folder:

    School Management System\School Management System\

Choose "Replace files in destination" when Windows asks.

Then Visual Studio -> Package Manager Console:

    Add-Migration M17_BackupRestoreAuditLogs
    Update-Database

Then:

    Clean Solution
    Rebuild Solution
    Run

No NuGet package is added by M17. Microsoft.Data.SqlClient is already available through the
SQL Server EF Core provider used by this project.

DEFAULT BACKUP SETTINGS
-----------------------
M17 works without changing appsettings.json. Defaults are:

- Root path: App_Data/Backups under the application content root
- Daily scheduled backup: enabled
- Scheduled hour: 02:00 local time
- Time zone: Asia/Karachi (with Windows Pakistan Standard Time fallback)
- Retention: 30 days
- In-app restore: enabled

You can override these later by adding this optional section to appsettings.json:

"Backup": {
  "RootPath": "D:\\SchoolBackups",
  "ScheduledBackupsEnabled": true,
  "ScheduledHourLocal": 2,
  "RetentionDays": 30,
  "TimeZoneId": "Asia/Karachi",
  "AllowInAppRestore": true
}

PRODUCTION STORAGE NOTE
-----------------------
For LocalDB during development, App_Data/Backups is normally writable by the current Windows user.
For IIS + a separate SQL Server service, SQL Server itself must be able to WRITE to the configured
backup directory and READ it during restore. A production path such as D:\SchoolBackups or a secured
network backup share can be configured, with NTFS/share permissions granted to the SQL Server service
account. Do not put backup files inside wwwroot.

The current M17 keeps the provider independent: RootPath can be a local protected disk or a secured
mounted/network location. A vendor-specific encrypted cloud replication provider can be added later
without changing the BackupRecord/restore workflow. Do not upload raw .bak files to public storage.

ROLES / SECURITY
----------------
- Principal/Admin/SuperAdmin: can open Backup Center, create and verify backups, see backup/audit history.
- Principal/SuperAdmin: can delete a stored backup file and can perform restore.
- Restore additionally requires the logged-in user's current password, RESTORE confirmation text,
  a reason, integrity verification and an automatic safety backup.
- Backup files are not exposed as public URLs and there is intentionally no anonymous download endpoint.
- Backup file paths are checked to ensure restore/delete only operate inside the configured backup root.

HOW TO TEST — BASIC
-------------------
1. Log in as Principal.
2. Open Administration -> Backup & Audit.
3. Click "Create backup now".
4. Confirm a new record appears with Status = Succeeded and a .bak file exists under App_Data/Backups.
5. Click Verify.
6. Confirm Integrity becomes Verified.
7. Open Audit logs and search "Backup". Confirm Backup.Created and Backup.Verified events appear.

HOW TO TEST — SCHEDULED BACKUP
------------------------------
The background service checks every 30 minutes after application startup. It creates at most one scheduled
backup per local calendar day after the configured ScheduledHourLocal. For a quick development test, set
ScheduledHourLocal to the current hour in the optional Backup section, restart the app and wait for the check.
Do not keep changing the production schedule just for testing.

HOW TO TEST — RESTORE (USE A TEST DATABASE FIRST)
--------------------------------------------------
IMPORTANT: first test restore on Development/Staging, not the school's only production database.

1. Create and Verify backup A.
2. Make a harmless test change after backup A (for example create a temporary notice).
3. Return to Backup & Audit -> Restore on backup A.
4. Enter a clear reason of at least 10 characters.
5. Enter the current Principal/SuperAdmin password.
6. Type exactly: RESTORE
7. Confirm the final browser prompt.
8. The system will:
   a) verify backup A;
   b) create a SafetyBeforeRestore backup of the current database;
   c) put the database in SINGLE_USER temporarily;
   d) restore backup A with CHECKSUM;
   e) return database to MULTI_USER;
   f) re-register recovery metadata and write restore history/audit.
9. Confirm the test change made after backup A is no longer present.
10. Confirm Backup & Audit shows a successful Restore History row and the safety backup file is retained.

If restore fails, M17 attempts to return the database to MULTI_USER. Keep the safety .bak and inspect the
error before trying again.

AUDIT LOG IMPROVEMENT
---------------------
M17 adds OldValues and NewValues fields so future modules can log before/after snapshots. Existing M01-M16
calls remain compatible; their existing Details field is preserved. New/updated sensitive workflows can
start passing oldValues/newValues to IAuditService.WriteAsync without creating a second audit system.

FILES ADDED
-----------
Options/BackupOptions.cs
Models/BackupType.cs
Models/BackupStatus.cs
Models/BackupRecord.cs
Models/RestoreRecord.cs
ViewModels/BackupViewModels.cs
Services/IBackupService.cs
Services/BackupService.cs
Services/BackupHostedService.cs
Controllers/BackupController.cs
Views/Backup/Index.cshtml
Views/Backup/Restore.cshtml
Views/Backup/AuditLogs.cshtml
README_M17.txt

FILES UPDATED
-------------
Program.cs
Data/ApplicationDbContext.cs
Models/AuditLog.cs
Services/IAuditService.cs
Services/AuditService.cs
Views/Shared/_AppNavigation.cshtml
Views/Reports/Index.cshtml

DEFINITION OF DONE CHECKLIST
----------------------------
[ ] Migration created and Update-Database succeeds.
[ ] Solution rebuilds without error.
[ ] Backup Center opens for leadership users.
[ ] Manual backup creates a real .bak file.
[ ] Verify passes RESTORE VERIFYONLY and SHA-256 comparison.
[ ] Failed backup shows a useful failure status/message instead of a fake success.
[ ] Scheduled job creates no duplicate daily scheduled backup.
[ ] Backup file is outside public wwwroot.
[ ] Principal/SuperAdmin password + RESTORE confirmation are required for restore.
[ ] Safety backup is created before restore.
[ ] Restore tested successfully on a test/staging database.
[ ] Audit screen records backup/verify/restore/delete actions.
[ ] Existing M01-M16 modules still open and work normally.

BUILD NOTE
----------
The generation environment used to prepare this patch does not contain the .NET 9 SDK, so dotnet build
could not be executed here. Run Rebuild Solution in Visual Studio after the migration. If Visual Studio
reports an exact compiler/runtime error, send the screenshot/error text before moving to M18.
