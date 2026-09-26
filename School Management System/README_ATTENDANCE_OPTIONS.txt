ATTENDANCE OPTIONS INSIDE MODULE - UI PATCH

This patch adds visible Attendance Methods cards inside Student Attendance:
- Manual Attendance
- Camera Attendance
- Face Enrollment (authorized admin roles)
- Biometric Attendance / Devices & Scan Events (authorized admin roles)
- Monthly Attendance Report

INSTALL
1. Extract this ZIP into:
   D:\School Management System\School Management System\
2. Choose Replace files in destination.
3. No migration is required.
4. Clean Solution, Rebuild Solution, then Run.
5. Open Attendance from the sidebar. The options now appear at the top of the Attendance page.

Existing controllers/routes used:
- Attendance/Index
- Attendance/MonthlyReport
- CameraAttendance/Index
- CameraAttendance/Enroll
- AttendanceIntegrations/Index

No backend/database logic is changed by this patch.
