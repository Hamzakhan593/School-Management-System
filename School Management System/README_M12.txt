M12 — STAFF ATTENDANCE & PAYROLL
================================
School Management Software — ASP.NET Core MVC 9 / EF Core 9 / SQL Server

PREREQUISITE
------------
Install/apply Modules M01 through M11 first. M12 reuses the M11 Staff entity and the M07 BiometricDevice infrastructure.

INSTALLATION
------------
1. Close the running application.
2. Extract this ZIP into the INNER project folder:

   School Management System\School Management System\

3. Choose "Replace files in destination" when Windows asks.
4. Open the solution in Visual Studio.
5. Open Package Manager Console and run:

   Add-Migration M12_StaffAttendancePayroll
   Update-Database

6. Rebuild Solution and run the application.

WHAT M12 ADDS
-------------
STAFF ATTENDANCE
- Daily staff attendance by date.
- Present, Absent, Late, Leave, Half Day and Holiday statuses.
- Mark All Present for quick office entry.
- Attendance source tracking: Manual, Biometric and Camera (plus existing integration source values).
- Check-in/check-out timestamps for device/camera events.
- Monthly staff attendance report.
- Existing M07 biometric devices can be mapped to staff members.
- Secure biometric bridge endpoint:
      POST /api/staff-attendance/bridge/events
  using the existing M07 X-Device-Key authentication.
- Repeated biometric scans are de-duplicated.
- Unmatched/leave/holiday events are retained for review instead of silently overwriting attendance.
- Camera page provides camera-assisted VISUAL confirmation by an authorized user. It does not upload/store the video and does not pretend to perform face recognition.

SALARY / ADVANCES
- Per-employee salary structure linked to M11 Staff.
- Basic salary.
- House, medical, transport and other fixed allowances.
- Fixed deduction.
- Optional deduction rates for Absent, Half Day, Late and Leave attendance.
- Advance/loan register with original amount, outstanding balance and monthly installment.
- Advance/loan cancellation preserves payroll history.

PAYROLL WORKFLOW
- One payroll run per school/month (duplicate protected).
- Generate Preview -> Recalculate -> Validate -> Approve -> Post.
- Payroll is calculated from current salary rules, attendance and active advance/loan installments.
- Draft-only manual allowance/deduction adjustments with notes.
- Invalid/missing salary structures block validation.
- Deductions greater than gross pay block validation.
- Approval and posting are separate stages.
- Payment method and payment/batch reference are stored when posting.
- Posting deducts approved advance/loan installments and automatically settles a fully repaid advance/loan.
- Posted payroll becomes historical; payslips are generated from the posted snapshot.
- Payroll dashboard shows pending approved runs and current-month posted payroll.
- Department totals and employee salary history are available.
- PDF payslips are available after posting.

AUTHORIZATION
-------------
- Staff Attendance: SuperAdmin, Principal, Admin, HR.
- Payroll: SuperAdmin, Principal, Admin, Accountant, HR.
- Payroll approval is restricted to SuperAdmin, Principal, Admin and HR.
- Sensitive salary/payroll/attendance actions write to the existing AuditLog service.

SUGGESTED FIRST TEST
--------------------
1. Login as Principal/Admin/HR.
2. Confirm M11 contains at least one active Staff record.
3. Staff / HR -> open employee -> Attendance & Payroll -> Salary Setup.
4. Enter Basic Salary and optional allowances/deduction rules; Save.
5. Open Staff Attendance and save several attendance days for the same month.
6. Payroll / Salaries -> select that month -> Generate Preview.
7. Open the run. Verify salary, attendance counts and deductions.
8. Recalculate Preview.
9. Validate Payroll.
10. Approve Payroll.
11. Select payment method/reference and Post Payroll.
12. Download the employee Payslip PDF.
13. Open Salary History and confirm the posted payroll is present.

ADVANCE / LOAN TEST
-------------------
1. Open employee Salary Setup.
2. Add an Advance or Loan with amount and monthly installment.
3. Generate/recalculate a future Draft payroll run.
4. Confirm the installment appears as Advance/Loan Deduction.
5. Post the run and confirm Outstanding Balance decreases.

BIOMETRIC STAFF TEST
--------------------
1. M07 must already have an active Biometric Device and its API key.
2. Staff Attendance -> Biometric Mapping.
3. Map a staff member to the device user reference.
4. Post a device event to /api/staff-attendance/bridge/events using the M07 request format and X-Device-Key.
5. Confirm the employee is marked Present and the event is not duplicated if the same EventId is sent again.

IMPORTANT NOTES
---------------
- Do not create a second DbContext or duplicate Staff entity. M12 extends the existing cumulative project.
- Payroll posting is intentionally irreversible from the normal UI. Corrections should be handled through a formal adjustment/reversal workflow in a future accounting enhancement rather than deleting posted salary history.
- Camera attendance remains a manual verification fallback. A future approved face-recognition provider can be integrated separately without changing the payroll/attendance data model.
- Back up the database before applying production migrations.

FILES REPLACED/EXTENDED
-----------------------
Program.cs
Data\ApplicationDbContext.cs
Models\Staff.cs
Views\Shared\_Layout.cshtml
Views\Staff\Details.cshtml

M12 adds new Models, ViewModels, Services, Controllers, StaffAttendance views and Payroll views.
