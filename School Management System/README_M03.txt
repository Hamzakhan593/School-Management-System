M03 — ADMISSIONS & ENQUIRIES
============================
Target: ASP.NET Core MVC 9 + EF Core 9 + SQL Server
Requires: M01 and M02 already copied, migrated and working.

BLUEPRINT SCOPE IMPLEMENTED
---------------------------
1. Admission Enquiry Register
   - Student name, parent/guardian, phone/email, desired class, source/referral and follow-up date
   - Stages: New, Contacted, Visit Scheduled, Test Scheduled, Approved, Rejected, Admitted
   - Search/filter by student/guardian/phone, stage and desired class
   - Follow-up due count and class-wise enquiry/admission statistics

2. Admission Form
   - Student personal data
   - Guardian data
   - Previous school/class/result remarks
   - Emergency contact
   - Academic session and desired class
   - Secure document upload for B-Form/CNIC, previous result, leaving certificate, photo, admission form and other documents
   - Files are stored outside wwwroot under App_Data/AdmissionDocuments; only authorized Admissions users can download them

3. Enquiry -> Permanent Student Conversion
   - Enquiry must first be Approved
   - Create admission form -> review/upload docs -> Submit -> Approve & Admit
   - Final admission creates Student, Guardian, StudentGuardian and StudentDocument records
   - Generates admission number in blueprint style: STD-2026-0001
   - Uses a per-school/per-year counter and Serializable transaction to reduce duplicate/concurrent admission risk
   - Repeated final submit does not intentionally create another student
   - Enquiry stage becomes Admitted automatically

4. Admission Register Export
   - CSV export of admitted students with admission number, class text, session and guardian details

5. Security / Audit
   - Access: SuperAdmin, Principal, Admin, Receptionist
   - SchoolId filtering prevents normal cross-school access
   - Important enquiry/application/document/admission actions are added to AuditLog
   - No hard-delete workflow for admitted student records

IMPORTANT DEPENDENCY NOTES FROM THE MASTER BLUEPRINT
----------------------------------------------------
The blueprint numbers M03 before M05 Classes/Sections/Subjects and M08 Fees/Challans, although the full admission workflow eventually depends on those shared modules.

To avoid creating duplicate temporary systems:
- M03 stores DesiredClass as admission/enquiry text for now. M05 will introduce the official Class/Section entities and placement mapping.
- M03 creates the permanent Student/Guardian core records now because conversion to a Student is explicitly part of M03. M04 will build the complete Student Management profile/search/history UI on these records instead of creating duplicate Student tables.
- M03 does NOT create a second fee/challan engine. Actual admission fee/challan posting will be connected in M08 Fees, Challans & Receipts, which is the blueprint's shared financial module.

COPY / INSTALL
--------------
1. Confirm M01 and M02 are working first.
2. Close the running project.
3. Copy ALL files/folders from this ZIP into:

   School Management System\School Management System\

4. Choose "Replace the files in the destination" when Windows asks.
5. Do not paste into the OUTER solution folder.

DATABASE MIGRATION
------------------
Visual Studio -> Tools -> NuGet Package Manager -> Package Manager Console.
Default project: School Management System

Run:

Add-Migration M03_AdmissionsEnquiries
Update-Database

CLI alternative:

dotnet ef migrations add M03_AdmissionsEnquiries
dotnet ef database update

FIRST TEST FLOW
---------------
1. Run project and login as Principal/Admin/Receptionist.
2. Make sure School Profile exists and at least one Academic Session exists.
3. Open Admissions -> + New Enquiry.
4. Create an enquiry, e.g.:
   Student: Ali Ahmed
   Guardian: Ahmed Khan
   Desired Class: Grade 6
   Stage: New
5. Open it and Edit -> change Stage to Approved.
6. Click Start Admission.
7. Complete Date of Birth, session, guardian and other details.
8. Create Admission Form.
9. On Application Details upload sample PDF/JPG/PNG documents if desired.
10. Click Submit Application.
11. Click Approve & Admit Student.
12. Confirm an admission number such as STD-2026-0001 appears.
13. Return to Enquiry Register: stage should now be Admitted.
14. Open Admission Applications and verify the admitted record.
15. Click Export Admission Register and confirm CSV downloads.

MANUAL TEST CHECKLIST
---------------------
[ ] Receptionist can open Admissions but unauthorized roles cannot.
[ ] Enquiry can be created and edited.
[ ] Search/stage/class filters work.
[ ] Follow-up due count changes correctly.
[ ] Enquiry cannot be manually set to Admitted.
[ ] Start Admission is available only after enquiry is Approved.
[ ] Duplicate application for the same enquiry is blocked.
[ ] Application requires a valid Draft/Active academic session.
[ ] DOB after/equal to admission date is blocked.
[ ] PDF/JPG/JPEG/PNG up to 5 MB uploads successfully.
[ ] Other file extensions / files above 5 MB are rejected.
[ ] Submitted application can no longer be edited/uploaded to.
[ ] Approve & Admit creates a unique STD-YEAR-#### admission number.
[ ] Re-clicking final admission does not create a second student.
[ ] Enquiry changes automatically to Admitted.
[ ] Admission register CSV contains admitted records.
[ ] Admission actions appear in AuditLog.

DO NOT
------
- Do not manually create SQL tables. Use EF Core migration.
- Do not create a second DbContext.
- Do not create a separate temporary Student table in M04; M04 must extend the Student/Guardian records introduced here.
- Do not create fee/challan tables in M03; M08 owns the financial engine.
