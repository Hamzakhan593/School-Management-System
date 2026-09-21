M05 — CLASSES, SECTIONS & SUBJECTS
=================================
Target: ASP.NET Core MVC 9 + EF Core 9 + SQL Server
Requires: M01, M02, M03 and M04 already copied, migrated and working.

BLUEPRINT SCOPE IMPLEMENTED
---------------------------
1. Official Class Master
   - Class name, optional code, sort order, active/inactive status
   - School-scoped unique class names
   - Edit/deactivate instead of destructive delete

2. Sections
   - Section name, class, capacity, classroom and optional class teacher
   - Section names are unique within a class
   - Capacity is enforced when a student is moved into a section

3. Subject Master
   - Subject code and title
   - Theory/practical flags
   - Default maximum and pass marks
   - Subject code/title duplicate protection

4. Class-Subject Mapping Per Academic Session
   - Assign subjects to a class for a selected academic session
   - Prevent duplicate class-subject-session mappings
   - Optional max/pass mark override; otherwise Subject defaults are used

5. Teacher-Subject-Class Assignment
   - Uses M01 users who have the Teacher role
   - Assign teacher to class + subject + optional section + academic session
   - Duplicate protection
   - Disabled assignments remain historical instead of being hard deleted

6. Groups / Streams
   - Optional groups such as Science, Arts or Computer
   - Group belongs to an official class

7. Class Roster
   - View current students by class or class/section and academic session
   - Admin/Principal can jump directly to Transfer / Change Placement
   - Teacher can access only rosters allowed by class/subject assignment

8. Student Placement Normalization
   - StudentEnrollment now has SchoolClassId, SectionId and AcademicGroupId
   - M03/M04 ClassName/SectionName/GroupStream remain as snapshots so old history is never destroyed
   - New placement uses only official M05 master data
   - Changing class/section closes the old StudentEnrollment record and creates a new one
   - This provides full class/section transfer history

9. Legacy M03/M04 Mapping
   - Classes/sections entered before M05 remain intact
   - Academic Structure page shows the number of unmapped placement rows
   - "Map matching legacy placements" links rows where names match the new masters
   - Unmatched rows are left unchanged for manual review; nothing is deleted

10. Admission Integration
   - New M03 admissions after M05 automatically link StudentEnrollment.SchoolClassId when DesiredClass exactly matches an active M05 class name
   - If it does not match, the text placement is preserved and can be mapped later

SECURITY / AUDIT
----------------
- Setup changes: SuperAdmin, Principal, Admin only
- Teachers can view Academic Structure but cannot create/edit setup
- Teacher roster access is limited to assigned class/subject scope
- SchoolId filtering is applied throughout
- Create/update/mapping/assignment actions write AuditLog entries
- Anti-forgery validation is applied to state-changing forms

COPY / INSTALL
--------------
1. Confirm M01-M04 are already working.
2. Close the running project.
3. Copy ALL files/folders from this ZIP into:

   School Management System\School Management System\

4. Choose "Replace the files in the destination".
5. Do NOT paste into the OUTER solution folder.

DATABASE MIGRATION
------------------
Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console
Default project: School Management System

Run:

Add-Migration M05_ClassesSectionsSubjects
Update-Database

CLI alternative:

dotnet ef migrations add M05_ClassesSectionsSubjects
dotnet ef database update

IMPORTANT FIRST-TIME SETUP ORDER
--------------------------------
After migration:
1. Login as Principal/Admin.
2. Open Classes & Subjects.
3. Create all official classes first, using names that match any existing M03/M04 class text where possible.
   Example: if existing StudentEnrollment says "Grade 6", create the master as exactly "Grade 6".
4. Add sections for those classes.
5. Add groups/streams if required.
6. Add subjects.
7. Select an academic session and map subjects to classes.
8. If Teacher users already exist, create teacher assignments.
9. Click "Map matching legacy placements" once.
10. Open Students -> student profile -> Change Placement and verify dropdowns now use official masters.

FIRST TEST FLOW
---------------
1. Create class "Grade 6" code "G6".
2. Add Section A, capacity 40, classroom 6-A.
3. Add optional group "Computer".
4. Add Subject: MATH / Mathematics / Theory / Max 100 / Pass 40.
5. Select active academic session.
6. Assign Mathematics to Grade 6.
7. Ensure at least one M01 user has Teacher role, then assign that teacher to Grade 6 + Mathematics.
8. Open an existing student's Change Placement screen.
9. Select official Grade 6 + Section A + optional group, enter roll number and save.
10. Return to Classes & Subjects -> Grade 6 -> Section A roster; student must appear.
11. Change the same student from Section A to another section using a later Effective From date.
12. Confirm old StudentEnrollment remains under Student History while roster shows only the new current section.
13. Login as Teacher and verify assigned roster opens but an unrelated class roster returns Access Denied/Forbidden.

MANUAL TEST CHECKLIST
---------------------
[ ] M01-M04 still work after M05 files are copied.
[ ] Migration creates SchoolClasses, Sections, Subjects, AcademicGroups, ClassSubjects and TeacherAssignments.
[ ] StudentEnrollments receives nullable SchoolClassId/SectionId/AcademicGroupId without losing old data.
[ ] Duplicate class names are blocked.
[ ] Duplicate section name in same class is blocked.
[ ] Duplicate subject code/title is blocked.
[ ] Subject pass marks cannot exceed max marks.
[ ] Class-subject duplicate mapping is blocked.
[ ] Teacher duplicate assignment is blocked.
[ ] Only Teacher-role school users can be assigned as teachers.
[ ] Principal/Admin can create and edit classes, sections and subjects.
[ ] Teacher cannot post setup changes.
[ ] Teacher roster access is scoped to assignments.
[ ] Section capacity blocks an additional transfer when full.
[ ] New student placement must use an official active class.
[ ] Selected section/group must belong to selected class.
[ ] Previous placement remains preserved after transfer.
[ ] Legacy placement mapping does not delete unmatched data.
[ ] New admission links SchoolClassId when DesiredClass matches an active class master.
[ ] AuditLog receives M05 setup/mapping/assignment actions.

DO NOT
------
- Do not delete old ClassName/SectionName snapshot columns. They preserve readable student history.
- Do not manually create SQL tables. Use the EF Core migration.
- Do not assign staff users as teachers unless they have the Teacher role in M01.
- Do not build Attendance tables here; M06 will consume this M05 class/section structure.
- Do not build Exam/Result tables here; M09/M10 will consume Subject and ClassSubject.

ENVIRONMENT NOTE
----------------
The artifact environment used to prepare this ZIP does not have the .NET SDK installed, so dotnet build and EF migration execution could not be run here. The module was integrated against the exact M01-M04 source files generated for this project and statically checked for path/reference consistency. After copying M05, run Build Solution before Add-Migration.
