M10 — RESULTS & RESULT CARDS
School Management Software / ASP.NET Core MVC 9
================================================

PURPOSE
-------
This patch implements M10 on top of M01–M09. It uses the existing M09 exam/marks data and the M02 grading scheme. It does not replace unrelated modules.

INSTALLATION
------------
1. Make sure M01 through M09 are already copied into the same project and their migrations have been applied.
2. Extract this ZIP directly into the INNER project folder:

   School Management System\School Management System\

3. Choose "Replace files in destination" when Windows asks. The replaced files are intentionally updated integration files (ApplicationDbContext.cs, Program.cs and _Layout.cshtml plus the existing ResultService interface/implementation).
4. In Visual Studio Package Manager Console run:

   Add-Migration M10_ResultsResultCards
   Update-Database

5. Rebuild Solution and run the application.

MAIN FEATURES
-------------
- Automatic total / maximum / percentage calculation from M09 marks.
- Pass/fail based on subject pass marks and Absent/Exempt rules.
- Grade from the M02 default grading scheme, with safe fallback grades if a scheme is not configured.
- Class/section position calculation with tie handling.
- Session attendance percentage snapshot up to the exam end date.
- Class result sheet with PDF output.
- Subject-wise performance analysis: average, pass/fail/absent counts and pass rate.
- Individual result card on screen + PDF.
- Selected-student result-card PDF.
- Class/section bulk result-card PDF.
- Whole-exam bulk result-card PDF for all published eligible students the logged-in user may access.
- Result card includes school information, student/class/section/roll, subject marks, total, percentage, grade, final status, attendance, teacher remarks, position and signature spaces.
- Published results are stored in StudentResult as immutable-style versioned snapshots.
- Controlled correction workflow: published results cannot be silently overwritten. Start correction mode, reopen/edit/verify/lock the required M09 marks sheet, lock the exam again, then publish a NEW result version with a required correction reason. Old versions are retained as Superseded.
- Audit entries are written for result publication and correction start.
- Teacher access follows M05 class/section assignments; publishing/correction remains restricted to SuperAdmin/Principal/Admin/ExamController.

RECOMMENDED TEST FLOW
---------------------
A. PREPARE MARKS IN M09
1. Create/configure an exam and its class subjects.
2. Enter marks for every active student.
3. Submit, verify and lock every class/section subject marks sheet.
4. Click Lock Exam so status becomes MarksLocked.

B. PREVIEW AND PUBLISH
1. Open Results & Cards from the main navigation.
2. Open the exam's class result.
3. Check totals, percentage, grade, PASS/FAIL, attendance and position.
4. Optionally filter by section.
5. Publish results. Prefer publishing "All Sections" when you want class-wide position rather than section-only position.
6. Confirm the Exam status becomes Published and each row displays Version 1.

C. RESULT CARDS
1. Open one student's Card and download its PDF.
2. Tick two or more students and test Selected Result Cards PDF.
3. Test Bulk Result Cards PDF for the class/section.
4. From Results index, test All Result Cards PDF after results are published.
5. Test Class Result PDF and Subject Analysis.

D. CONTROLLED CORRECTION
1. On a published class result, enter a reason and click Start Correction.
2. In Exams & Marks, reopen only the required marks sheet.
3. Correct marks, then Submit -> Verify -> Lock that sheet.
4. Use Lock Exam again so the exam returns to MarksLocked.
5. Return to Results and use Publish Corrected Version with a reason.
6. Confirm current result becomes Version 2; Version 1 remains in the database as Superseded.

IMPORTANT RULES
---------------
- Do not manually delete published StudentResult rows.
- Do not edit historical result values directly in SQL Server.
- The current result is identified by IsCurrent = true; older versions remain for audit/history.
- Financial modules are not touched by this patch.
- M15 Annual Promotion can later consume StudentResult.IsPassed / IsCurrent as the final promotion eligibility input.

NEW DATABASE TABLE
------------------
StudentResults

KEY UPDATED INTEGRATION FILES
-----------------------------
Data/ApplicationDbContext.cs
Program.cs
Services/IResultService.cs
Services/ResultService.cs
Views/Shared/_Layout.cshtml

NEW FILE GROUPS
---------------
Models/StudentResult.cs
Models/StudentResultStatus.cs
ViewModels/ResultViewModels.cs
Services/IResultPdfService.cs
Services/ResultPdfService.cs
Controllers/ResultsController.cs
Views/Results/*

BUILD NOTE
----------
The file-generation environment used to prepare this patch does not contain the .NET SDK, so the final compilation must be performed in Visual Studio on your development computer. The patch was assembled against the cumulative M01–M09 project structure and checked for project-relative integration consistency.
