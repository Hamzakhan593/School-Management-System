M15 — ANNUAL PROMOTION / SESSION ROLLOVER
School Management System — Patch ZIP
============================================================

PURPOSE
-------
M15 implements the year-end academic-session rollover described in the Master Development Blueprint.
It creates the next session without overwriting historical student placements, copies reusable academic/fee setup, prepares a class-wise promotion preview, and commits the final decisions as one controlled transaction.

This patch is designed for the project after M01–M14 have already been applied.

INSTALLATION
------------
1. Close the running application.
2. Extract M15_Annual_Promotion_Session_Rollover.zip directly inside:

   School Management System\School Management System\

3. Choose "Replace files in destination" when Windows asks.
4. Open the solution in Visual Studio.
5. Open Package Manager Console and run:

   Add-Migration M15_AnnualPromotionSessionRollover
   Update-Database

6. Rebuild Solution.
7. Run the application.

DATABASE / MIGRATION
--------------------
M15 adds two tracked tables:
- PromotionBatches
- PromotionItems

The migration also adds their indexes, relationships and audit-safe references.
Do not manually edit the database tables.

ACCESS
------
Only the existing M01 management roles can operate annual rollover:
- SuperAdmin
- Principal
- Admin

Open:
Administration → Annual Promotion

NORMAL YEAR-END FLOW
--------------------
1. Open Annual Promotion.
2. Click Start New Rollover.
3. Choose the current/source academic session.
4. Enter the next session name and dates.
5. Choose whether to carry forward class-subject mappings and fee structures.
6. Select only the teacher assignments that should continue next year.
7. Create the next session.
8. Review the generated student preview.
9. For every student choose one final decision:
   - Promote
   - Repeat
   - Hold
   - Graduate
   - Transfer Out
10. Save Reviewed Decisions.
11. Click Commit Student Rollover.
12. Verify the result.
13. Click Finalize & Activate New Session when ready.

WHAT "COPY SETUP" MEANS IN THIS PROJECT
---------------------------------------
The current M05 schema stores Class, Section and Subject as school-wide master records, not separate rows per academic session. M15 therefore reuses those same master records instead of creating duplicate classes/sections/subjects.

The session-specific setup is carried forward by copying:
- ClassSubject mappings
- Active fee structures
- Selected teacher assignments

M15 also copies Terms and Grading Schemes as supporting next-session setup because fee/result configuration can depend on them.

PROMOTION PROPOSAL LOGIC
------------------------
The automatic proposal is guidance only. Authorized staff can override it before commit.

Current proposal rules:
- Latest published/current result is Fail → Repeat
- Passed student with a higher class available → Promote
- Passed student already in the highest configured class → Graduate
- Missing/ambiguous result or placement → Hold

Target class ordering uses SchoolClass.SortOrder.
For promotion, M15 tries to keep the same section/group name in the next class when such a matching option exists.

IMPORTANT: The system never treats the automatic proposal as the final decision. The preview is the required human review step.

HOW HOLD WORKS
--------------
Hold is a valid rollover decision.
A held student is carried into the target academic session in the same class/section/group and receives StudentEnrollmentStatus.Held.

This is intentional:
- the old session can still be archived cleanly;
- the student remains traceable in the new session;
- normal attendance/fees/exam flows that require Active enrollment will not treat the held placement as fully active;
- an authorized user can later resolve the student through Students → Change Placement, creating a new Active placement while keeping the Held row in history.

TRANSACTION SAFETY
------------------
Commit Student Rollover is transactional.
The system first validates the entire batch, then closes source enrollments and creates target-session enrollments inside one database transaction.

If any database operation fails, the whole commit rolls back instead of leaving half of a class promoted.

The module also protects against:
- duplicate rollover batches for the same source session;
- duplicate target-session enrollments;
- students added/changed after preview creation;
- invalid class/section/group combinations;
- committing against a target session that is no longer Draft.

SAFE ROLLBACK
-------------
Before finalization, a committed rollover can be rolled back if the target-session students have not yet received operational transactions.

Rollback is blocked if affected target students already have:
- attendance records;
- marks/results;
- fee challans/transactions.

When rollback is safe, M15:
- removes the target enrollments created by the batch;
- restores the exact original Student status;
- restores the exact original StudentEnrollment status;
- restores the original source enrollment as current;
- keeps the new target session/setup as Draft so the preview can be rebuilt.

FINALIZATION
------------
Finalize & Activate New Session performs the session lifecycle transition safely:
- source Active session → Closed;
- target Draft session → Active;
- source Closed session → Archived;
- rollover batch → Finalized.

The sequence is saved inside a transaction so the existing one-active-session database rule remains valid.
Historical source-session records are not deleted and remain available for reports.

AUDIT LOGGING
-------------
M15 writes audit events for:
- rollover creation;
- preview preparation;
- reviewed decision updates;
- commit;
- rollback;
- finalization.

MANUAL TEST CHECKLIST
---------------------
[ ] M01–M14 migrations are already applied.
[ ] Principal/Admin can open Administration → Annual Promotion.
[ ] Teacher/Accountant/Receptionist cannot operate M15 URLs.
[ ] Start New Rollover lists the current Active/Closed session.
[ ] Changing the source session reloads its teacher-assignment choices.
[ ] Creating rollover creates one Draft target AcademicSession.
[ ] Terms and grading schemes are copied into the target session.
[ ] Class-subject mappings copy only when the option is enabled.
[ ] Fee structures copy only when the option is enabled.
[ ] Only selected teacher assignments are carried forward.
[ ] A second rollover cannot be created for the same source session.
[ ] Preview loads all current eligible source-session students.
[ ] Latest result guidance displays where a published/current result exists.
[ ] Passed student proposes next class when a higher SortOrder exists.
[ ] Failed student proposes Repeat.
[ ] Final-class passed student proposes Graduate.
[ ] Missing/ambiguous result proposes Hold.
[ ] Admin can override Promote / Repeat / Hold / Graduate / Transfer Out.
[ ] Promote/Repeat target class and section are validated.
[ ] Hold retains the current class/section/group for the target session.
[ ] Saving preview does not change StudentEnrollment history.
[ ] Commit creates new target StudentEnrollment rows rather than overwriting source rows.
[ ] Promote and Repeat target enrollments are Active.
[ ] Hold target enrollment is Held.
[ ] Graduate updates Student status to Graduated.
[ ] Transfer Out updates Student status to Transferred.
[ ] Source enrollment remains preserved with an end date and historical rollover status.
[ ] Commit cannot create duplicate target enrollments.
[ ] Commit fails safely if student placement/status changed after preview.
[ ] Rollback works before target attendance/marks/results/fees exist.
[ ] Rollback is blocked after target operational records exist.
[ ] Finalize activates the new session and archives the old session.
[ ] Old-session student history and reports remain readable.
[ ] Audit Activity shows M15 rollover events.
[ ] Existing M01–M14 pages still work after M15 installation.

FILES ADDED / REPLACED
----------------------
Models\PromotionDecision.cs
Models\PromotionBatchStatus.cs
Models\PromotionItemStatus.cs
Models\PromotionBatch.cs
Models\PromotionItem.cs
ViewModels\PromotionViewModels.cs
Services\IPromotionService.cs
Services\PromotionService.cs
Controllers\PromotionsController.cs
Views\Promotions\Index.cshtml
Views\Promotions\Create.cshtml
Views\Promotions\Preview.cshtml
Data\ApplicationDbContext.cs
Program.cs
Views\Shared\_AppNavigation.cshtml
Views\Reports\Index.cshtml
README_M15.txt

DEPENDENCIES
------------
M15 intentionally builds on earlier modules, especially:
- M02 School Profile & Academic Session
- M04 Student Management
- M05 Classes, Sections & Subjects
- M08 Fees, Challans & Receipts
- M10 Results & Result Cards
- M11 Staff / HR
- M14 Dashboard & Reports shell

NOTE ABOUT BUILD VALIDATION
---------------------------
The generation environment does not contain the .NET 9 SDK, so a real dotnet build cannot be executed here.
The patch was checked structurally against the merged M01–M14 source. After copying the files and applying the migration, use Visual Studio → Rebuild Solution as the final compile check.
