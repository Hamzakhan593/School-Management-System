M13 — EXPENSES & FINANCIAL LEDGER
=================================
Target: School Management System / ASP.NET Core MVC 9 / EF Core 9 / SQL Server
Dependency: M01 through M12 must already be applied.

WHAT THIS PATCH ADDS
--------------------
1. Expense Categories
   - Default categories are seeded for existing schools on application startup:
     Utilities, Maintenance, Stationery, Transport, Rent, Internet, Cleaning, Events, Miscellaneous.
   - Principal/Admin can add, edit, order, activate or deactivate categories.

2. Expense Entry
   - Date, category, amount, vendor/paid-to, payment method, reference and notes.
   - Optional receipt/bill attachment: PDF/JPG/JPEG/PNG, maximum 5 MB.
   - Attachments are stored under App_Data/ExpenseFiles, outside the public web root.
   - Existing records are cancelled rather than hard-deleted, preserving financial history.

3. High-Value Approval Workflow
   - Expenses below Rs. 50,000 are marked NotRequired for approval.
   - Expenses of Rs. 50,000 or more are Pending until Principal/Admin/SuperAdmin approves or rejects them.
   - Rejected and cancelled expenses are excluded from financial totals.
   - Editing an expense recalculates its approval requirement; a high-value edited item returns to Pending.
   - The Rs. 50,000 threshold is an M13 practical default. M19 Settings can later make it school-configurable.

4. Reports
   - Monthly expense report.
   - Category filter and category-wise totals.
   - Approved/countable, pending and rejected totals.

5. Other Recorded Income
   - Records non-fee income (donation, sale, rental, etc.).
   - Cancellation preserves the original record and requires a reason.

6. Simple Financial Ledger / Summary
   - Fee income: reads active FeePayment records from M08.
   - Other income: reads M13 OtherIncome records.
   - Payroll cost: reads POSTED payroll runs/items from M12.
   - Operating expenses: counts approved/not-required M13 expenses only.
   - Shows monthly Inflow, Outflow and Net plus a 12-month year summary.
   - This is intentionally NOT full double-entry accounting/GL, matching the blueprint.

7. Authorization and Audit
   - Expenses/ledger: Principal, Admin, Accountant, SuperAdmin.
   - Category management and high-value approval: Principal, Admin, SuperAdmin.
   - Create/update/approve/reject/cancel actions write AuditLog entries.

FILES TO COPY
-------------
Extract this ZIP into the INNER project folder:

School Management System\School Management System\

Choose "Replace files in destination" when Windows asks.
Do NOT extract it one level above the .csproj file.

DATABASE MIGRATION
------------------
After replacing the files, open Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console.
Make sure the Default Project is School Management System, then run:

Add-Migration M13_ExpensesFinancialLedger
Update-Database

Then:
1. Build -> Rebuild Solution
2. Run the application
3. Sign in as Principal/Admin/Accountant

MANUAL TEST CHECKLIST
---------------------
A. Categories
[ ] Open Expenses -> Categories as Principal/Admin.
[ ] Confirm default categories appear after the application has started once after migration.
[ ] Add a custom category and edit it.
[ ] Deactivate a category and confirm it cannot be chosen for a new expense.

B. Normal Expense
[ ] Record an expense below Rs. 50,000.
[ ] Confirm status is NotRequired and it is included in the monthly approved/countable total.
[ ] Upload a PDF/JPG/PNG receipt and download it from the expense list.
[ ] Edit the expense and confirm the update is saved.

C. High-Value Approval
[ ] As Accountant, create an expense for Rs. 50,000 or more.
[ ] Confirm it is Pending and excluded from countable expense totals/financial outflow.
[ ] As Principal/Admin, approve it and confirm it enters financial totals.
[ ] Create another high-value expense, reject it with a reason and confirm it stays excluded.

D. Cancellation
[ ] Cancel an expense with a reason.
[ ] Confirm the row remains visible as Cancelled but is excluded from financial totals.
[ ] Confirm a cancelled record cannot be edited.

E. Expense Report
[ ] Open Expense Report.
[ ] Filter by year/month/category.
[ ] Verify category totals equal the approved/not-required expense rows.

F. Other Income
[ ] Record non-fee income.
[ ] Confirm it appears in Other Income and Financial Summary.
[ ] Cancel it with a reason and verify it is removed from financial totals but its history remains.

G. Cross-Module Financial Summary
[ ] Receive a fee payment in M08 during the selected month.
[ ] Post a payroll run in M12 for the selected month.
[ ] Add an approved expense in M13.
[ ] Open Financial Ledger.
[ ] Confirm: Total Income = Fee Income + Other Income.
[ ] Confirm: Total Outflow = Posted Payroll + Approved/NotRequired Expenses.
[ ] Confirm Net = Total Income - Total Outflow.

IMPORTANT NOTES
---------------
- M13 depends on the actual M08 FeePayment tables and M12 PayrollRun/PayrollItem tables. Apply M01-M12 first.
- Financial rows are a management summary, not formal double-entry accounting.
- There is no hard-delete button for financial records. Use Cancel/Reject so history and auditability are preserved.
- Server-side authorization is applied; hiding a navigation link is not treated as security.
- I could not execute dotnet build in the generation environment because the .NET SDK is unavailable there. Run Rebuild Solution in Visual Studio immediately after migration.
