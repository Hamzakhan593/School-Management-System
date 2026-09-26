M11 — STAFF / HR
School Management Software — Module Patch
===========================================

PREREQUISITE
------------
Install M01 through M10 first. This ZIP is a patch: copy/extract its contents into the INNER ASP.NET project folder:

School Management System\School Management System\

Choose "Replace files in destination" when Windows asks.

DO NOT extract it one level above the .csproj file.

DATABASE UPDATE
---------------
After copying the files, open Visual Studio -> Package Manager Console and run:

Add-Migration M11_StaffHR
Update-Database

Then Build -> Rebuild Solution and run the project.

WHAT M11 ADDS
-------------
1. Staff / HR navigation for Principal, Admin, SuperAdmin and HR.
2. Staff list with search/filter by name, employee ID, CNIC, phone, designation, status, employment type and department.
3. Staff profile with automatically generated employee IDs in the form:
   EMP-2026-0001
4. Employment types:
   - Permanent
   - Contract
   - Visiting
   - Part Time
5. Staff status:
   - Active
   - On Leave
   - Inactive
   - Resigned
   - Terminated
   - Retired
6. Staff personal/employment information:
   - Name
   - CNIC
   - Phone/email/address
   - Designation
   - Department
   - Qualification
   - Joining date
   - Employment type
   - Notes
7. Staff photo upload.
8. Staff documents:
   - CNIC
   - Degree/Certificate
   - Contract
   - Appointment Letter
   - Experience Letter
   - Other
9. Controlled staff exit workflow. Resignation/termination/retirement records the exit date and reason instead of deleting history.
10. Existing M01 user accounts can be linked to a staff profile by Principal/Admin/SuperAdmin.
11. Existing M05 teacher/class/subject assignments are displayed inside the linked staff profile. No duplicate teacher-assignment table is created.
12. Audit logs are created for important Staff/HR actions.
13. Staff files are stored under App_Data/StaffFiles rather than in public wwwroot.
14. ApplicationUser.StaffId is now connected to the Staff table and protected by a unique relationship.
15. A SalaryStructureId placeholder is reserved on Staff. M12 will create the actual SalaryStructure, StaffAttendance, Payroll and Payslip records and connect them to this same staff profile.

IMPORTANT DESIGN NOTE
---------------------
M11 does NOT duplicate M05 teacher assignment logic. A staff member who teaches should be linked to an existing user account that has the Teacher role. Their classes/sections/subjects then appear automatically in the Staff profile because M05 already owns those assignments.

Similarly, staff attendance and salary/payroll are not duplicated here because they belong to M12. The M11 Staff entity is intentionally prepared for them so M12 can attach attendance and salary records without recreating employees.

QUICK TEST CHECKLIST
--------------------
1. Login as Principal/Admin/HR.
2. Open Staff / HR from the navigation.
3. Click Add Staff and create an employee.
4. Confirm an Employee ID such as EMP-2026-0001 is generated automatically.
5. Search the employee by name and employee ID.
6. Edit designation, department, qualification and contact information.
7. Upload a JPG/PNG staff photo.
8. Upload a PDF/JPG/PNG staff document and download it again.
9. Login as Principal/Admin, open the staff profile -> User Account tab and link an existing M01 user.
10. If that user has Teacher role and M05 assignments, check that the Teaching Assignments tab shows class/section/subject assignments.
11. Use Mark Exit and record Resigned/Terminated/Retired + date + reason. Confirm the employee is still visible in history/list filters.
12. Check the AuditLogs table for Staff.Created, Staff.Updated, Staff.UserLinked, Staff.DocumentUploaded and Staff.ExitRecorded events.

ROLE BEHAVIOR
-------------
Principal / Admin / SuperAdmin:
- Full Staff/HR profile management
- Link/unlink existing system user accounts

HR:
- Create/edit staff
- Photos/documents
- Record staff exit
- View teaching assignments
- Cannot change the M01 user-account link

Teacher/Accountant/Receptionist/ExamController:
- No M11 Staff/HR access by default

FILES REPLACED BY THIS PATCH
----------------------------
Data/ApplicationDbContext.cs
Program.cs
Views/Shared/_Layout.cshtml

NEW FILE GROUPS
---------------
Models/Staff*.cs
Services/IStaff*.cs
Services/Staff*.cs
Controllers/StaffController.cs
ViewModels/StaffViewModels.cs
Views/Staff/*
README_M11.txt

BUILD NOTE
----------
The current ChatGPT execution environment does not include the .NET SDK, so dotnet build cannot be executed here. The patch was prepared against the cumulative M01-M10 project structure. After extraction, run the migration commands above and Rebuild Solution in Visual Studio before moving to M12.
