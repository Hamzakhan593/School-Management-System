M19 — SETTINGS & MASTER DATA
School Management System | ASP.NET Core MVC 9 / EF Core 9 / SQL Server

WHAT THIS PATCH ADDS
--------------------
1. Central Settings & Master Data screen for Principal/Admin/SuperAdmin.
2. One school-specific SystemSetting record (one row per School).
3. Numbering settings:
   - Admission prefix + sequence digits
   - Challan prefix + receipt prefix + sequence digits
   - Fiscal year start month
4. Fee policy settings:
   - Default due day
   - Fixed late fee + grace days
   - Automatic late fee toggle
   - Automatic monthly challan generation + generation day
5. Attendance settings:
   - Teacher correction cutoff
   - Low-attendance threshold
   - Enable/disable Late, Leave, Half Day and No Class manual statuses
6. Results / printable settings:
   - Show/hide attendance and class position on result card
   - Result card footer
   - Payslip footer
   - General print footer
   - Principal/Class Teacher signature labels
   - Optional signature image uploads
7. Backup settings integrated with M17:
   - Scheduled backup on/off
   - Local backup hour
   - Retention days
   - In-app restore enable/disable
8. Security settings:
   - Per-school inactivity session timeout
   - Minimum password length
   - Digit / uppercase / lowercase / special-character requirements
9. Communication settings:
   - Email/SMS/WhatsApp on/off
   - Provider display names
   - Provider API keys/secrets are intentionally NOT stored in the database.
10. Feature toggles prepared for optional modules:
   - Biometric attendance
   - Camera attendance
   - Parent/student portal
   - Online payments
   - Push notifications
11. Audit entry whenever settings are changed.

INTEGRATION WITH EXISTING MODULES
---------------------------------
- M03 Admissions now uses the configured admission-number prefix/digits for NEW admissions.
- M08 Fees now uses configured challan/receipt prefixes, due-day and late-fee policy.
- M08 automatic monthly challan job now reads school settings instead of only appsettings.json.
- M06 Attendance now reads the configured cutoff, low-attendance threshold and allowed statuses.
- M10 result card HTML/PDF now follows result-card visibility/footer/signature-label settings.
- M12 payslip PDF uses the configured payslip footer.
- M17 scheduled backups/retention/restore toggle now read school settings.
- M16 external communication attempts respect enabled/disabled settings and provider names.
- M01 password changes/new users are validated against the school-specific password policy.
- M01 authenticated users are signed out after the configured period of inactivity.

IMPORTANT DESIGN RULES
----------------------
- Existing admission/challan/receipt numbers are NEVER rewritten when prefixes change.
- Grading schemes stay academic-session specific and are still managed from Academic Sessions.
  This protects historical result cards from silently changing.
- Fee Heads, Fee Structures and Student Discounts remain in M08 as their single source of truth.
- School logo, challan footer and receipt footer remain in School Profile (M02).
- External provider credentials/tokens must be stored in secure server configuration/environment
  secrets. M19 stores provider names and enable/disable choices only.

INSTALLATION
------------
1. Extract this ZIP into the INNER project folder:

   School Management System\School Management System\

2. Choose "Replace files in destination" when Windows asks.

3. Open Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console.

4. Run:

   Add-Migration M19_SettingsMasterData
   Update-Database

5. Clean Solution.
6. Rebuild Solution.
7. Run the project.

WHERE TO OPEN M19
-----------------
Login as Principal/Admin/SuperAdmin -> left sidebar -> Settings & Master Data.

RECOMMENDED MANUAL TEST
-----------------------
A. Open Settings & Master Data and save once. Confirm success message.
B. Change Admission Prefix to TEST and admit a NEW student. Confirm a number such as:
   TEST-2026-0001 (sequence depends on existing counter).
C. Change Challan/Receipt prefixes and create a NEW challan/payment. Confirm new numbers only.
D. Set Low Attendance Threshold to 80%. Open Monthly Attendance and verify low-attendance flag.
E. Disable "Late" status. Reload Student Attendance and verify Late is not offered for manual entry.
F. Change Result Card options/footer and open/download a published result card.
G. Add principal/class-teacher signature images and verify the HTML result card preview.
H. Change backup hour/retention and open Backup & Audit. Confirm M17 dashboard reflects them.
I. Change password policy, then create/reset a user password and verify the rule is enforced.
J. Set session timeout to a short safe test value (minimum 5 minutes), stay inactive, and confirm
   login is required again after timeout.
K. Enable an external notification channel with a provider name. M16 should show the provider name
   but still report NotConfigured until an actual adapter + secure credentials are installed.
L. Check Backup & Audit -> Audit Logs for Settings.Updated.

MIGRATION NOTE
--------------
M19 creates the SystemSettings table and a unique SchoolId index. The first settings row is created
when Settings are saved. Until then, all services use safe defaults compatible with earlier modules.

COMPATIBILITY
-------------
This patch assumes M01 through M18 have already been applied in order. Do not apply it to the fresh
original project without the earlier module patches.

BUILD NOTE
----------
The generation environment does not include the .NET 9 SDK, so a real dotnet build could not be run
here. The patch was checked structurally/staticly. Visual Studio Rebuild Solution is the final compile
verification on your machine.
