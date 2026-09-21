M06 — STUDENT ATTENDANCE
School Management Software — ASP.NET Core MVC 9 / EF Core 9 / SQL Server

DEPENDENCY
M01, M02, M03, M04 and M05 must already be pasted into the project and their migrations applied.
M06 uses M05 SchoolClass, Section, TeacherAssignment and normalized StudentEnrollment links.

WHERE TO COPY
Extract this ZIP into the INNER project folder that contains Program.cs and:
  School Management System.csproj
Choose "Replace files in destination" when Windows asks.

FILES ADDED / REPLACED
- Controllers/AttendanceController.cs
- Data/ApplicationDbContext.cs
- Models/StudentAttendance.cs
- Models/StudentAttendanceStatus.cs
- Models/AttendanceSource.cs
- Options/AttendanceOptions.cs
- Services/IAttendanceService.cs
- Services/AttendanceService.cs
- ViewModels/AttendanceMarkingViewModel.cs
- ViewModels/MonthlyAttendanceReportViewModel.cs
- Views/Attendance/Index.cshtml
- Views/Attendance/MonthlyReport.cshtml
- Views/Shared/_Layout.cshtml
- Program.cs
- appsettings.json

DATABASE
Open Visual Studio > Tools > NuGet Package Manager > Package Manager Console.
Make sure the correct project is selected, then run:

  Add-Migration M06_StudentAttendance
  Update-Database

Then Build > Build Solution and run the project.

WHAT M06 IMPLEMENTS
1. Daily student attendance by Academic Session + Class + Section + Date.
2. Statuses: Present, Absent, Late, Leave, Half Day, Holiday and No Class.
3. "Mark All Present" and "Mark All No Class" fast actions.
4. Manual attendance Source field now, ready for M07 Biometric/Camera/API sources later.
5. One attendance record per student per academic session/date (duplicate protected by unique DB index).
6. Attendance roster comes from StudentEnrollment and respects enrollment effective dates.
7. Teacher access is limited to classes/sections assigned in M05 (class teacher or teacher assignment).
8. Principal/Admin/SuperAdmin can manage all classes.
9. Teacher edit cutoff. Default: end of attendance day + 24 hours.
10. After cutoff, Teacher is read-only. Principal/Admin/SuperAdmin can correct, but must enter a correction reason.
11. Late corrections and normal class attendance saves are written to AuditLog.
12. Monthly attendance report with Present/Absent/Late/Leave/Half Day counts.
13. Low-attendance flag. Default threshold: below 75%.
14. Attendance source is preserved when editing an existing record, so future biometric/camera origin remains traceable.

CONFIGURATION
appsettings.json contains:

  "Attendance": {
    "TeacherEditCutoffHoursAfterDayEnd": 24,
    "LowAttendanceThresholdPercent": 75
  }

You can change these values later. M19 Settings & Master Data can move them into school-managed settings.

MONTHLY PERCENTAGE RULE USED IN M06
- Present = 1 attended day
- Late = 1 attended day
- Half Day = 0.5 attended day
- Absent = 0
- Leave = 0
- Holiday / No Class = excluded from countable days
Percentage = attended equivalent / countable marked days × 100

IMPORTANT M05 SETUP BEFORE TESTING A TEACHER
For a Teacher account to see a class/section in Attendance:
- assign the teacher through Classes & Subjects > Teacher Assignment, OR
- set that user as the Section Class Teacher.
Principal/Admin can access all classes without this assignment.

MANUAL TEST CHECKLIST
A. Principal/Admin
1. Login as Principal/Admin.
2. Ensure an active Academic Session exists.
3. Ensure a Class and Section exist in M05.
4. Ensure active students have M05 placements in that Class/Section.
5. Open Attendance from the top navigation.
6. Select Session, Class and Date > Load Roster.
7. If class has sections, select Section > Load Roster again.
8. Click Mark All Present.
9. Change one student to Absent, one to Late, one to Half Day.
10. Add an optional remark and Save Attendance.
11. Reload same date. Saved statuses must return; no duplicate records should be created.
12. Change one status and save again. It should update the same attendance row.

B. Teacher permissions
1. Create/use a Teacher user from M01.
2. In M05 assign that teacher to a class/section or make them section class teacher.
3. Login as Teacher.
4. Attendance should show only assigned classes/sections.
5. Manually typing another class/section URL must not allow attendance save.

C. Cutoff / correction
1. For easy testing temporarily set TeacherEditCutoffHoursAfterDayEnd = 0.
2. Choose an old attendance date inside the academic session.
3. Teacher should see the sheet as read-only.
4. Principal/Admin should be able to edit it, but Correction Reason is required.
5. Save and confirm an Attendance.LateCorrectionSaved entry appears in AuditLog table.

D. Monthly report
1. Open Attendance > Monthly Report.
2. Choose Session, Class, optional Section and Month.
3. Verify counts and percentages.
4. Students below the configured threshold should show a Low warning.

NOTES
- M06 is manual attendance only. M07 will add biometric/camera attendance integration on top of this stable attendance model.
- Financial posting is unrelated and is not modified by this module.
- Do not hard-delete historical attendance; corrections should update through authorized workflow and audit logs.
