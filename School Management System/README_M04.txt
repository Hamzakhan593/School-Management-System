M04 — STUDENT MANAGEMENT
========================
Target: ASP.NET Core MVC 9 + EF Core 9 + SQL Server
Requires: M01, M02 and M03 already copied, migrated and working.

BLUEPRINT SCOPE IMPLEMENTED
---------------------------
1. Complete Student Profile
   - Admission number (immutable in normal edit flow)
   - Name, father/guardian, gender, DOB, B-Form/CNIC, address, contact
   - Admission date and student status
   - Student photo upload/update
   - Current roll number shown from current academic placement

2. Guardian / Parent Details
   - Multiple guardians can be linked to one student
   - Name, relationship, phone, occupation, CNIC and address
   - One guardian can be marked Primary
   - Primary guardian name is reflected in the Student master profile

3. Academic Placement + History
   - New StudentEnrollment entity
   - Academic session, class, section, group/stream and roll number
   - Effective From / Effective To dates
   - One current placement per student (filtered unique index)
   - Changing placement closes the old record instead of overwriting it
   - Current roll number is synchronized to Student.RollNumber for quick search
   - New admissions created after M04 automatically receive their initial placement from M03 DesiredClass

4. Student Search
   - Search by student name
   - Admission number
   - Roll number
   - B-Form/CNIC
   - Parent/guardian phone
   - Filter by student status, current class and academic session

5. Student Profile Tabs
   - Overview: student + guardian data
   - Fees: reserved for M08 shared fee ledger
   - Attendance: reserved for M06 attendance data
   - Results: reserved for M09/M10 exams/results
   - Documents: upload/download/remove student documents
   - History: complete academic placement history

6. Documents & Photo Security
   - New files stored under App_Data/StudentFiles, outside wwwroot
   - PDF/JPG/JPEG/PNG only, max 5 MB for documents
   - JPG/JPEG/PNG only, max 5 MB for profile photo
   - M03 admission documents remain readable through the student profile
   - Removing an M03-origin document removes the StudentDocument link but does not destroy the original admission file, preserving admission history

7. Security / Audit / Concurrency
   - M04 Student Management access: SuperAdmin, Principal, Admin, Receptionist
   - Teacher access is intentionally NOT opened yet because M05/M06 must first provide assigned-class scoping; this avoids exposing full student documents/identity data to every teacher
   - SchoolId filtering on all student/guardian/document actions
   - RowVersion concurrency protection on student profile edit
   - AuditLog entries for profile edits, placement changes, guardian changes, document changes and photo updates

IMPORTANT M04 -> M05 DEPENDENCY NOTE
------------------------------------
The master blueprint's final data model uses official Class and Section entities, but M05 — Classes, Sections & Subjects is the next numbered module and is not installed yet.

Therefore M04 deliberately stores ClassName and SectionName inside StudentEnrollment so M03/M04 can work now without inventing duplicate temporary Class tables. M05 should introduce the official Class/Section/Subject masters and then map/normalize these placement values without deleting enrollment history.

This follows the existing M03 design, where DesiredClass is text until M05 is available.

COPY / INSTALL
--------------
1. Confirm M01, M02 and M03 are working first.
2. Close the running project.
3. Copy ALL files/folders from this ZIP into:

   School Management System\School Management System\

4. Choose "Replace the files in the destination" when Windows asks.
5. Do NOT paste into the OUTER solution folder.

DATABASE MIGRATION
------------------
Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console.
Default project: School Management System

Run:

Add-Migration M04_StudentManagement
Update-Database

CLI alternative:

dotnet ef migrations add M04_StudentManagement
dotnet ef database update

IMPORTANT FOR STUDENTS ADMITTED BEFORE M04
------------------------------------------
Students already admitted while only M03 was installed do not yet have StudentEnrollment rows because that table did not exist at that time.

For each existing test student:
Students -> Open Profile -> Change Placement -> select the academic session -> enter current class/section/roll -> Save.

All NEW admissions completed after M04 is installed automatically create the initial StudentEnrollment from the admission application's AcademicSession + DesiredClass.

FIRST TEST FLOW
---------------
1. Build Solution after copying M04.
2. Run Add-Migration M04_StudentManagement and Update-Database.
3. Login as Principal/Admin/Receptionist.
4. Open Students from the top navigation.
5. Open an existing admitted student's profile.
6. If the student came from M03 before M04, click Change Placement:
   - Session: choose current session
   - Class: Grade 6
   - Section: A
   - Roll Number: 12
   - Effective From: a date inside that academic session
   - Save
7. Confirm class, section, roll and session now appear on the profile/list.
8. Change placement again using a later effective date and verify the old placement remains under History.
9. Edit profile and update contact/status.
10. Add or edit a guardian and mark Primary.
11. Upload a PDF/JPG/PNG document and download it.
12. Upload/update a student photo.
13. Search the Students list using admission number, roll number, B-Form or parent phone.
14. Test Status/Class/Session filters.
15. Create a NEW admission through M03 and approve it; open the new Student profile and verify its initial DesiredClass/session is already in History.

MANUAL TEST CHECKLIST
---------------------
[ ] M01/M02/M03 still open and work after M04 files are copied.
[ ] M04 migration creates StudentEnrollments without duplicate DbContext/entities.
[ ] Principal/Admin/Receptionist can open Students.
[ ] Unauthorized roles cannot directly browse /Students.
[ ] Search by name works.
[ ] Search by admission number works.
[ ] Search by roll number works.
[ ] Search by B-Form/CNIC works.
[ ] Search by guardian phone works.
[ ] Current class/session filters work.
[ ] Student permanent details can be edited.
[ ] Invalid DOB/admission date is rejected.
[ ] Concurrent profile update shows a conflict instead of silently overwriting.
[ ] Guardian can be added/edited and Primary guardian can be changed.
[ ] New academic placement closes the previous current placement.
[ ] Old placement remains visible in History.
[ ] Database blocks more than one IsCurrent=true placement for the same student.
[ ] Document upload blocks unsupported extensions / files above 5 MB.
[ ] Documents download only through authorized Student Management routes.
[ ] M03-origin admission documents remain downloadable.
[ ] Photo update works and is not exposed from wwwroot.
[ ] New M03 admission automatically creates initial StudentEnrollment.
[ ] AuditLog receives M04 sensitive actions.

DO NOT
------
- Do not manually create StudentEnrollment SQL tables; use the EF Core migration.
- Do not delete old StudentEnrollment rows when a student changes class/session.
- Do not create separate Class/Section master tables inside M04; M05 owns those official masters.
- Do not build fee, attendance or result tables in M04; their profile tabs intentionally connect later to M06/M08/M09/M10.
- Do not give unrestricted student-document access to Teacher role before assigned-class scoping is available.

ENVIRONMENT NOTE
----------------
The current artifact environment does not have the .NET SDK installed, so dotnet build / EF migration execution could not be run here. The module was prepared against the M01-M03 source structure and statically checked for file/reference consistency. After copying it, run Build Solution in Visual Studio before creating the migration.
