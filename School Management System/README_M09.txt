M09 — EXAMINATIONS & MARKS
School Management Software — ASP.NET Core MVC 9 / EF Core 9 / SQL Server

PURPOSE
This patch implements Module M09 from the Master Development Blueprint. It assumes M01 through M08 have already been copied into the same project and their migrations have been applied.

WHERE TO COPY
Extract this ZIP into the INNER project folder:

School Management System\School Management System\

Choose “Replace files in destination” when Windows asks. The ZIP contains only new/changed M09 files and keeps the existing project-relative folder paths.

DATABASE
After copying the files, open Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console and run:

Add-Migration M09_ExaminationsMarks
Update-Database

Then Build -> Rebuild Solution and run the project.

WHAT M09 ADDS
1. Exam setup
   - title, type, academic session, optional term/semester
   - start/end dates
   - Draft / MarksEntryOpen / MarksLocked / Published-ready status model
   - class assignment to an exam

2. Exam subject configuration
   - uses the official M05 class-subject mapping
   - maximum marks and pass marks
   - optional theory/practical component maximums
   - configurable weightage percentage
   - duplicate subject/class protection

3. Marks entry
   - class/section + subject-wise marks sheet
   - teacher access restricted by M05 TeacherAssignment
   - Principal/Admin/ExamController can review all exam marks
   - Absent and Exempt are stored separately from numeric marks
   - server-side validation prevents marks above configured maximum
   - theory + practical component validation
   - student roster is taken from the exam academic session/class/section

4. Marks workflow
   Draft -> Submitted -> Verified -> Locked

   - teacher saves Draft marks
   - teacher submits a complete sheet for verification
   - Exam Controller / Principal / Admin verifies
   - authorized manager locks the sheet
   - locked sheets cannot be edited silently
   - authorized manager can reopen a sheet only with a reason; action is audited
   - whole exam can be changed to MarksLocked only when every expected class/section/subject sheet is locked

5. Shared result calculation foundation
   - IResultService + ResultService are added now so M10 Result Cards uses one calculation source
   - handles configured subject weightage
   - Absent counts as failed/zero for that subject
   - Exempt subjects are excluded from total possible marks
   - M10 will add grading, result snapshots, publishing, class result sheets and result-card PDFs

6. Audit trail
   Exam creation/update, class assignment, subject setup, marks save, submit, verify, lock and reopen actions use the existing M01 AuditLog infrastructure.

MAIN NEW DATABASE TABLES
- Exams
- ExamClasses
- ExamSubjects
- ExamMarksSheets
- StudentMarks

IMPORTANT PRE-REQUISITES BEFORE TESTING
A. M02: School Profile + an academic session must exist.
B. M04/M05: Students must have normalized StudentEnrollment rows with SchoolClassId (and SectionId when the class uses sections).
C. M05: Class-subject mappings must exist for the selected academic session.
D. If testing as Teacher, M05 TeacherAssignment must map that teacher to the correct session/class/subject and optionally section.

QUICK MANUAL TEST
1. Login as Principal/Admin/ExamController.
2. Open “Exams & Marks” from the top navigation.
3. Click Create Exam.
4. Choose the active academic session, enter title/type/dates and save.
5. On Exam Setup, assign at least one class.
6. Add exam subjects from the M05 class-subject mappings.
7. Check Max Marks / Pass Marks. For a theory+practical subject, set component maximums whose total equals Max Marks.
8. Click “Open Marks Entry”. After this, exam setup is intentionally locked.
9. Click “Enter / Review Marks” for a subject.
10. Select section if needed.
11. Enter marks or set student status to Absent/Exempt.
12. Click Save Draft Marks.
13. Click Submit for Verification.
14. Login/use Principal/Admin/ExamController and Verify Marks.
15. Lock the marks sheet.
16. Repeat for every subject/section.
17. Return to Exam Setup and click Lock Whole Exam.
18. The exam should become MarksLocked only after all expected sheets are locked.

TEACHER PERMISSION TEST
- Teacher should see exams only where the teacher has an active M05 assignment for a class in that exam.
- Teacher can enter only assigned subject/class/section marks.
- Teacher can Save Draft and Submit.
- Teacher cannot Verify, Lock, reopen, edit exam setup or lock the whole exam.

VALIDATION TESTS
- Enter marks above Max Marks -> save must fail.
- Enter theory/practical above component max -> save must fail.
- Set component maximums where Theory + Practical != Total Max -> configuration must fail.
- Submit without saving every active student row -> submit must fail.
- Try editing a submitted/verified/locked sheet as teacher -> must be blocked.
- Try locking a Submitted sheet before Verify -> must fail.
- Try Lock Whole Exam before every expected sheet is Locked -> must fail.
- Reopen a locked sheet without reason -> must fail.

NOT INCLUDED IN M09 BY DESIGN
The blueprint places these in M10 or later:
- final grade / pass-fail result snapshots
- class positions / merit
- result publishing
- result cards / class result sheet PDFs
- bulk result-card generation
- parent portal result publication

The blueprint says Excel marks import can be added later, so M09 keeps it out of the core first implementation to reduce duplicate/invalid mark posting risk.

IF BUILD FAILS
Do not create duplicate Exam/StudentMark classes manually. First confirm M01-M08 were copied in order and that the latest M08 versions of Program.cs, ApplicationDbContext.cs and _Layout.cshtml existed before replacing them with M09 versions.

This environment did not have the .NET SDK available, so the patch was structurally/static checked here but could not be compiled with dotnet build. Always run Rebuild Solution after copying and before testing.
