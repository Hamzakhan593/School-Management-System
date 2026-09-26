SCHOOL MANAGEMENT SYSTEM — M08 FEES, CHALLANS & RECEIPTS
=========================================================

Prerequisite
------------
M01 through M07 should already be installed in the same project and their migrations applied.
This ZIP is a PATCH for the existing project. Do not create a second project.

Where to extract
----------------
Extract the contents of this ZIP into the INNER project folder:

School Management System\School Management System\

Choose "Replace the files in the destination" when Windows asks.
The ZIP contains only M08 new/changed files and keeps the same project-relative folders.
It intentionally does NOT replace appsettings.json, so your existing database connection string/configuration is preserved.

Database migration
------------------
In Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console, make sure the School Management System project is selected, then run:

Add-Migration M08_FeesChallansReceipts
Update-Database

Then Build -> Build Solution and run the application.

Recommended first-time setup flow
---------------------------------
1. Login as Principal/Admin.
2. Open Fees & Challans -> Fee Heads.
3. Create fee heads such as Tuition, Admission, Annual, Examination, Transport, Lab, Computer, Library, Fine, Miscellaneous.
4. Open Fee Structures and define class-wise or student-specific amounts for the active academic session.
5. Add discounts/scholarships where required.
6. Open Generate Challans.
7. Select academic session + billing month + scope.
8. Click Preview first. Review Ready / Existing challan skipped / No applicable fee structure rows.
9. Commit only after preview is correct.
10. Open Challans, download/print PDF challans, then use Collect Payment for a student.
11. Print the generated receipt and verify the Student Ledger and Defaulters report.

Implemented in M08
------------------
- Fee Heads with custom codes, default amount/frequency and active/inactive state.
- Class-wise fee structures.
- Student-specific fee structures/overrides.
- Monthly, One-Time, Term-Based and Custom fee frequencies.
- Fixed-amount and percentage student discounts/scholarships with date range and approval note.
- Individual student challan generation.
- Class / Section challan generation.
- Selected-student challan generation.
- Whole-school challan generation.
- Preview-before-commit workflow.
- Duplicate/idempotency protection for batch generation.
- Unique CH-yyyy-000001 style challan numbering.
- Previous outstanding snapshot shown on new challans while old debt remains on its original challans/ledger.
- Challan statuses: Draft/Issued/Partially Paid/Paid/Overdue/Waived/Cancelled model support.
- Regenerate challan with version/history protection (only after active payments are reversed).
- Cancellation by authorized management with reason; financial history is retained.
- Cash, Bank Transfer, Card/Online, Cheque and Other payment methods.
- Partial/full payment support.
- Oldest-due-first automatic payment allocation, down to challan item level where possible.
- Unique RC-yyyy-000001 receipt numbering.
- Payment reversal instead of deleting posted payments.
- Student fee ledger.
- Defaulter report with filters and CSV export.
- Daily collection report with payment-method, cashier/user and class summaries.
- Server-generated printable PDF challans, batch challan PDF and receipts without requiring an extra PDF NuGet package.
- Audit events for sensitive fee configuration, challan and payment actions.
- Optional late-fee policy.
- Optional scheduled monthly whole-school challan generation background service.
- M03 admission screen integration: an admitted student can be sent directly to challan generation.
- M04 student Fees tab integration: finance users can open the ledger or collect payment.

Roles
-----
Fees dashboard, challans, collections and reports:
- SuperAdmin
- Principal
- Admin
- Accountant

Fee Heads / Fee Structures / Discounts and sensitive cancellation/regeneration/reversal controls are restricted to management roles as implemented by controller authorization.

Optional configuration
----------------------
The module works without changing appsettings.json. Automatic monthly generation is OFF by default and automatic late fee is 0 by default.

If you want to configure these later, merge the following top-level section into appsettings.json (do not remove your existing ConnectionStrings/Attendance settings):

"Fees": {
  "AutoGenerateEnabled": false,
  "MonthlyGenerationDay": 1,
  "DefaultDueDay": 10,
  "LateFeeFixedAmount": 0,
  "LateFeeGraceDays": 0,
  "ApplyLateFeeOnCollection": true,
  "SchoolTimeZoneId": "Asia/Karachi"
}

Set AutoGenerateEnabled=true only after fee structures are verified. The background job uses an idempotent monthly batch key so the same monthly automatic batch is not duplicated.

Manual test checklist
---------------------
[ ] Build succeeds after migration.
[ ] Existing M01-M07 pages still open normally.
[ ] Principal/Admin can create Fee Heads.
[ ] Duplicate fee-head code is blocked.
[ ] Class monthly fee structure can be saved.
[ ] Student-specific fee structure can be saved.
[ ] Percentage/fixed discount can be assigned.
[ ] Generate -> Preview shows expected students and total.
[ ] Clicking Commit after preview creates challans.
[ ] Repeating the SAME commit token does not create duplicates.
[ ] A new preview for the same student/month skips an existing active challan.
[ ] Individual, class/section, selected students and whole-school scopes work.
[ ] Challan PDF opens/downloads.
[ ] Batch PDF opens/downloads.
[ ] Partial payment updates challan balance/status and ledger.
[ ] Full payment changes challan status to Paid.
[ ] Payment above total outstanding is rejected.
[ ] Receipt PDF opens/downloads.
[ ] Reversing a payment restores the outstanding balance and keeps the payment record.
[ ] Cancelling a challan with active payment is blocked until payment is reversed.
[ ] Defaulters report lists overdue balances and CSV export works.
[ ] Daily Collection totals match active (non-reversed) receipts.
[ ] Student Details -> Fees links work for authorized finance roles.
[ ] Admission Details -> Generate fee/challan link works for an admitted student.

Important financial rules
-------------------------
- Money uses decimal precision, not floating point.
- Posted payments are reversed, not hard-deleted.
- Repeated batch submission is protected against duplicate challans.
- Financial history is retained when challans are cancelled/regenerated.
- Scheduled generation is deliberately disabled by default until the school's real fee structure is configured and tested.

PDF note
--------
M08 uses a small internal server-side PDF writer so no extra package is required. It is intended for clean office printing. School branding/template polish, bank copy layouts and optional QR/barcode can be enhanced later without changing the fee ledger logic.

Build verification note
-----------------------
The delivery environment used to prepare this patch did not have the .NET SDK installed, so dotnet build / EF migration execution could not be run here. Static source checks were performed. After copying the patch, run the migration and Visual Studio Build Solution before moving to M09.
