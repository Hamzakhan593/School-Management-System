M16 — Documents, Notices & Communication
=========================================

PURPOSE
-------
This patch implements Module M16 from the School Management Software Master Development Blueprint.
It is designed to be applied AFTER M01–M15.

INSTALL LOCATION
----------------
Extract this ZIP into the inner project folder:

School Management System\School Management System\

Choose "Replace files in destination" when Windows asks.
The ZIP preserves project-relative paths and contains only M16 new/changed files.

DATABASE MIGRATION
------------------
Open Visual Studio > Tools > NuGet Package Manager > Package Manager Console and run:

Add-Migration M16_DocumentsNoticesCommunication
Update-Database

Then:
1. Clean Solution
2. Rebuild Solution
3. Run the application

WHAT M16 ADDS
-------------
1. Notice Board
   - Create/edit notices
   - Audience: All, Staff, Parents, Students, Class
   - Optional class + section target
   - Draft / Publish / Unpublish / Archive workflow
   - Publish-from and publish-until window
   - Optional attachment
   - Archive instead of destructive delete

2. School Documents
   - Fee Policy
   - Exam Timetable
   - Circular
   - Form
   - Policy
   - General / Other documents
   - Audience filtering
   - Optional class/section targeting
   - Effective date window
   - Active/Hidden status
   - Controlled download through an authorized controller action

3. Secure File Handling
   - Files stored under App_Data/Communications, not directly exposed under wwwroot
   - Random generated storage filenames
   - Original filename retained for download
   - 10 MB file limit
   - Allowed: PDF, DOC, DOCX, XLS, XLSX, JPG, JPEG, PNG
   - Path traversal protection

4. Communication History
   - In-App, Email, SMS and WhatsApp channels
   - Audience + estimated recipient count
   - Provider name/status/detail
   - Timestamped history
   - Audit log integration

5. Provider-Agnostic External Messaging
   - In-App notice-board delivery works immediately.
   - Email/SMS/WhatsApp DO NOT pretend to send messages when no provider is configured.
   - Those attempts are saved as NotConfigured in communication history.
   - A future approved provider can be plugged into ICommunicationDispatcher without changing notices/documents.
   - Provider credentials/configuration belong in M19 Settings / integration configuration, not hard-coded source files.

6. Roles
   - Every authenticated user can open the published Notices & Documents page.
   - Management actions: SuperAdmin, Principal, Admin, Receptionist, ExamController.
   - Other staff see audience-appropriate published content only.

IMPORTANT PARENT/STUDENT PORTAL NOTE
------------------------------------
The current Identity model from M01–M15 has StaffId linking for staff accounts, but it does not yet contain a reliable StudentId/GuardianId identity link for the optional Parent/Student portal.
For privacy, M16 therefore does NOT expose class-targeted notices to generic Parent/Student accounts when the system cannot prove which class they belong to.
Class targeting is stored correctly now and is ready for the later Parent/Student portal identity mapping. Do not weaken this check by showing every class notice to every parent/student.

MANUAL TEST CHECKLIST
---------------------
A. Setup
[ ] M01–M15 migrations are already applied.
[ ] Run Add-Migration M16_DocumentsNoticesCommunication.
[ ] Run Update-Database.
[ ] Rebuild solution successfully.

B. Navigation
[ ] Login as Principal/Admin.
[ ] Sidebar shows "Notices & Documents".
[ ] Open it and confirm the professional notice/document center loads.
[ ] Click "Manage Communications".

C. Notice workflow
[ ] Create an All-audience notice.
[ ] Create a Staff notice.
[ ] Create a Class notice and select class/optional section.
[ ] Save as draft.
[ ] Publish it.
[ ] Confirm it appears on Notices & Documents.
[ ] Unpublish and confirm it disappears.
[ ] Publish again.
[ ] Upload a PDF/JPG attachment and confirm secure download works.
[ ] Try an unsupported file type and confirm validation blocks it.
[ ] Archive notice and confirm it no longer appears publicly but remains in management history.

D. Visibility dates
[ ] Create a notice with future Visible From date and confirm it is not shown yet.
[ ] Create a notice with expired Visible Until date and confirm it is not shown.
[ ] Verify invalid end-before-start dates are blocked.

E. Documents
[ ] Upload Fee Policy PDF.
[ ] Upload Exam Timetable.
[ ] Upload a class-targeted circular.
[ ] Download an active document.
[ ] Hide it and confirm it disappears from normal document center.
[ ] Reactivate it and confirm it returns.
[ ] Replace the document file while editing and verify new download.

F. Communication dispatch
[ ] Open a published notice > Dispatch.
[ ] Choose InApp and dispatch.
[ ] Confirm history status is Sent and provider is Built-in notice board.
[ ] Choose Email/SMS/WhatsApp.
[ ] Confirm the system records NotConfigured instead of claiming a real message was sent.
[ ] Confirm estimated recipient count is recorded.

G. Authorization
[ ] Login as normal Teacher and confirm published All/Staff content can be viewed.
[ ] Confirm Teacher cannot open management create/edit actions.
[ ] Login as Receptionist and confirm communication management is available.
[ ] If Parent/Student demo users exist, verify they do not receive unrelated class-targeted items.

H. Audit
[ ] Open audit activity/report and confirm notice/document publish/archive/dispatch actions are recorded.

DATABASE ENTITIES ADDED
-----------------------
Notice
SchoolDocument
CommunicationHistory

NEW SERVICE ABSTRACTIONS
------------------------
ICommunicationFileService / CommunicationFileService
ICommunicationDispatcher / CommunicationDispatcher

M17 NEXT
--------
The next module in the blueprint is:
M17 — Backup, Restore & Audit Logs.
