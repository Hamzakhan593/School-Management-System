M14 — DASHBOARDS & REPORTS
School Management System — Patch ZIP
============================================================

PURPOSE
-------
This module upgrades the dashboard from the temporary M01 role page to a professional, data-driven school management dashboard and adds a centralized Reports Center.

This patch is designed for the project after M01–M13 have already been applied.

INSTALLATION
------------
1. Close the running application.
2. Extract M14_Dashboards_Reports_Professional.zip directly inside:

   School Management System\School Management System\

3. Choose "Replace files in destination" when Windows asks.
4. Open the solution in Visual Studio.
5. Clean Solution, then Rebuild Solution.
6. Run the application.

DATABASE / MIGRATION
--------------------
NO NEW DATABASE TABLES OR COLUMNS are introduced by M14.
Therefore DO NOT create an M14 migration.

You do NOT need to run:
  Add-Migration M14...
  Update-Database

M14 reads the data already created by M01–M13.

WHAT M14 ADDS
-------------
1. Professional responsive application shell
   - Desktop sidebar
   - Mobile off-canvas navigation
   - School branding area
   - Role-aware navigation
   - Modern profile menu and top bar
   - Responsive tables/cards
   - Print-friendly reports

2. Professional live dashboard
   - Active student count
   - Active staff count
   - Today's attendance percentage
   - Present / absent / late counts
   - Unmarked attendance sections
   - Low-attendance warning count
   - Current-month expected fee
   - Current-month collection
   - Outstanding fee
   - Collection rate
   - Fee defaulter count
   - Current-month expenses
   - Posted payroll
   - Pending expense/payroll approvals
   - New admissions and enquiry pipeline
   - Active exams / published results
   - Teacher assignment/student metrics
   - Six-month fee collection visual
   - Seven-day attendance trend
   - Class-strength visual
   - Expense category breakdown
   - Recent payments
   - Recent admissions
   - Recent audited activity
   - Priority operational alerts
   - Quick-action shortcuts

3. Role-aware dashboard experience
   - Principal/Admin: complete executive overview
   - Accountant: fees, outstanding, expenses and financial actions
   - Teacher: attendance, assignments, students and exam flow
   - Receptionist: admissions/enquiries/student workflow
   - Exam Controller: exams/results workflow
   - HR: staff/payroll workflow
   - Parent/Student: protected portal placeholder; confidential school-wide data is not exposed

4. Reports Center
   - Executive Summary (printable)
   - Active Student Register
   - Student Register CSV export
   - Enquiry / Admission links
   - Student Attendance report link
   - Staff Attendance report link
   - Fee Collection report
   - Defaulters report
   - Student fee ledger
   - Challan status/batch report
   - Expense report
   - Monthly financial summary
   - Exam/results reporting hub
   - Staff register
   - Payroll summary
   - Searchable Audit Activity report

5. Backup status
   - The Reports Center visibly marks Backup Status as an M17 dependency.
   - It does not pretend backup monitoring exists before the Backup/Restore module is built.

IMPORTANT ROLE TEST
-------------------
Login with the different M01 roles and confirm users only see navigation and dashboard areas relevant to their job.

Principal / Admin:
- Dashboard should show attendance + fee + alerts + class strength + finance + recent activity.
- Reports Center should expose the full management report set.

Accountant:
- Dashboard should show fees, outstanding balances and expenses.
- Should not get Staff HR creation links.

Teacher:
- Dashboard should show attendance, active exams, teaching assignments and assigned students.
- Finance data should not be visible.

Receptionist:
- Dashboard should show active students, enquiries, pending applications and new admissions.

HR:
- Dashboard should show staff and payroll information.
- Finance/student fee data should not be visible.

Exam Controller:
- Dashboard should focus on active exams and result publication metrics.

MANUAL TEST CHECKLIST
---------------------
[ ] Principal dashboard loads without error.
[ ] Dashboard values change based on existing M03–M13 data.
[ ] Sidebar active state follows the current module.
[ ] Sidebar collapses to a mobile drawer below desktop width.
[ ] Reports Center opens.
[ ] Active Student Register opens and filters by class/search.
[ ] Student Register CSV downloads.
[ ] Executive Summary prints cleanly.
[ ] Audit Activity filters by search/action/date.
[ ] Fee/attendance/exam/staff report cards route to the existing module reports.
[ ] Accountant cannot access Staff/HR-only pages through M14 navigation.
[ ] Teacher cannot see finance cards.
[ ] Receptionist cannot see finance/payroll controls.
[ ] Parent/Student do not see confidential school-wide metrics.
[ ] Existing M01–M13 pages still open inside the new professional shell.

FILES ADDED / REPLACED
----------------------
Controllers\DashboardController.cs
Controllers\ReportsController.cs
Services\IDashboardService.cs
Services\DashboardService.cs
ViewModels\DashboardViewModels.cs
ViewModels\ReportViewModels.cs
Views\Dashboard\Index.cshtml
Views\Reports\Index.cshtml
Views\Reports\Executive.cshtml
Views\Reports\StudentRegister.cshtml
Views\Reports\AuditActivity.cshtml
Views\Shared\_Layout.cshtml
Views\Shared\_AppNavigation.cshtml
Views\Shared\_SvgSprite.cshtml
wwwroot\css\site.css
wwwroot\js\dashboard.js
Program.cs
README_M14.txt

NOTE ABOUT BUILD VALIDATION
---------------------------
The generation environment does not contain the .NET 9 SDK, so a real dotnet build cannot be executed here. The patch was checked structurally against the merged M01–M13 source. After copying the files, Visual Studio Rebuild Solution is the final compile check.
