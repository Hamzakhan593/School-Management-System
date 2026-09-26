School Management System - Database FK Fix 02

Problem fixed:
SQL Server Error 1785: AttendanceEvents foreign keys may cause cycles or multiple cascade paths.

What changed:
- AttendanceEvent -> Student: SetNull -> NoAction
- AttendanceEvent -> StudentEnrollment: SetNull -> NoAction
- AttendanceEvent -> StudentAttendance: SetNull -> NoAction
- AttendanceEvent -> BiometricDevice: SetNull -> NoAction
- StaffAttendanceEvent -> Staff: SetNull -> NoAction
- StaffAttendanceEvent -> StaffAttendance: SetNull -> NoAction
- StaffAttendanceEvent -> BiometricDevice: SetNull -> NoAction

Why:
Attendance/event records are historical/audit data. They should not be silently altered or removed when a related
student/staff/device record is deleted. NoAction also avoids SQL Server multiple-cascade-path errors.

INSTALL:
1. Extract this ZIP into:
   D:\School Management System\School Management System\
2. Choose "Replace files in destination".
3. In Package Manager Console run:
   Remove-Migration
   Add-Migration M19_SettingsMasterData
   Update-Database

IMPORTANT:
- Run Remove-Migration only if the failed M19 migration still exists locally and is NOT recorded as applied in
  __EFMigrationsHistory.
- Your previous Update-Database failed while creating AttendanceEvents, so EF's migration transaction normally
  rolls back the failed migration. If Remove-Migration says it cannot remove an applied migration, STOP and send
  that exact message before doing anything else.
- After Update-Database succeeds, Rebuild Solution.
