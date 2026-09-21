M02 — SCHOOL PROFILE & ACADEMIC SESSION
=======================================
Target: ASP.NET Core MVC 9 + EF Core 9 + SQL Server
Requires: M01 Authentication, Users & Roles already copied and migrated.

WHAT THIS MODULE ADDS
---------------------
1. School Profile
   - School name, registration no, address, phone, email, principal name
   - School logo upload (PNG/JPG/JPEG/WEBP, max 2 MB)
   - Challan footer and receipt footer text
   - Links existing M01 users to the school in the first single-school setup

2. Academic Session lifecycle
   - Draft -> Active -> Closed -> Archived
   - Database + service protection so only one Active session exists per school
   - Closed/Archived sessions are read-only for normal Admin
   - Principal/SuperAdmin may make authorized historical corrections; actions are audited

3. Session setup
   - Terms / semesters
   - Working days per week
   - Holidays / school calendar entries
   - Default grading scheme and grade percentage rules

4. M01 integration
   - Program.cs registers school/session services
   - ApplicationDbContext adds M02 tables and relationships
   - UserManagementController now assigns new users to the current SchoolId and limits normal managers to their own school
   - Main navigation adds School Profile and Academic Sessions

COPY / INSTALL
--------------
1. Close the running project.
2. Open this ZIP.
3. Copy ALL files/folders into:

   School Management System\School Management System\

4. Choose "Replace the files in the destination" when Windows asks.
5. Do NOT copy into the outer solution folder by mistake. Program.cs and the .csproj live in the INNER project folder.

DATABASE MIGRATION
------------------
After M01 migration already exists, open Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console.
Make sure the Default project is "School Management System" and run:

Add-Migration M02_SchoolProfileAcademicSession
Update-Database

If using CLI instead:

dotnet ef migrations add M02_SchoolProfileAcademicSession
dotnet ef database update

FIRST USER FLOW
---------------
1. Run the project.
2. Login with your M01 Principal account.
3. Open "School Profile" from navigation.
4. Enter the real/demo school information and Save.
5. Existing users with no SchoolId are linked to this school (appropriate for the current first single-school version).
6. Open "Academic Sessions" -> "+ New Session".
7. Create e.g. 2026-2027. It is saved as Draft.
8. Open Setup and add:
   - Terms / semesters
   - Holidays/calendar entries
   - Grade rules
9. Click Activate. The database/service prevents a second Active session for the same school.
10. At year/session end, Close it. Principal can Archive a Closed session.

MANUAL TEST CHECKLIST
---------------------
[ ] School Profile saves successfully.
[ ] Logo validation rejects >2 MB and non-image extensions.
[ ] Principal user receives SchoolId after first school save.
[ ] Existing unassigned M01 users are linked to the first school.
[ ] New M01 users created afterward inherit the manager's SchoolId.
[ ] Academic session can be created only after School Profile is configured.
[ ] Duplicate session name is blocked for the same school.
[ ] End date before/equal start date is blocked.
[ ] Term dates outside session dates are blocked.
[ ] Holiday dates outside session dates are blocked.
[ ] Overlapping grading percentage ranges are blocked.
[ ] Draft session can be activated.
[ ] A second session cannot become Active while another one is Active.
[ ] Active session can be Closed.
[ ] Closed session is not editable by normal Admin.
[ ] Principal/SuperAdmin can perform audited historical corrections.
[ ] Closed session can be Archived by Principal/SuperAdmin.
[ ] School/session changes create AuditLog records.

IMPORTANT
---------
- Do not create another DbContext. M02 extends the M01 ApplicationDbContext.
- Do not manually create SQL tables. Use EF Core migration.
- M03 Admissions & Enquiries should be installed only after this module is working.
