School Management System - Compile Error Fix Patch 01

Fixes the errors reported in:
1. ViewModels/FeesViewModels.cs
   - CS0117: DataType.Month does not exist.
   - Replaced DataType.Month with the supported custom data type string "month".

2. Views/Attendance/Index.cshtml
3. Views/Attendance/MonthlyReport.cshtml
4. Views/Exams/Marks.cshtml
5. Views/Staff/Details.cshtml
   - Razor interpreted the loop variable named "section" as the Razor @section directive.
   - Renamed the loop variable to "sec" and rewrote the inline Marks section selector more clearly.

INSTALL
Extract/copy the folders directly into the INNER project folder:
D:\School Management System\School Management System\
Choose Replace files in destination.

Then in Visual Studio:
1. Build > Clean Solution
2. Close Visual Studio if stale Razor errors remain.
3. Delete bin and obj folders (optional but recommended after Razor parser errors).
4. Reopen Visual Studio.
5. Build > Rebuild Solution

No database migration is required for this patch.
