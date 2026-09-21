M01 — AUTHENTICATION, USERS & ROLES
School Management Software / ASP.NET Core MVC 9

WHAT THIS ZIP DOES
- Adds ASP.NET Core Identity + SQL Server/EF Core.
- Adds ApplicationUser with FullName, IsActive, SchoolId/StaffId placeholders and ForcePasswordChange.
- Adds roles: SuperAdmin, Principal, Admin, Accountant, Teacher, ExamController, HR, Receptionist, Parent, Student.
- Adds Login / Logout / Forgot Password / Reset Password / Change Password.
- Adds 5-attempt lockout for 15 minutes and 30-minute sliding login session.
- Adds role-based dashboard redirect.
- Adds Principal/Admin/SuperAdmin user-management screens.
- Adds create/edit/activate/deactivate/role assignment and temporary password reset.
- Adds basic AuditLog + audit service for sensitive M01 actions.

HOW TO PLACE FILES
1. Close the running project in Visual Studio if needed.
2. Open your project folder:
   School Management System\School Management System\
3. Extract/copy ALL files from this M01 ZIP into that folder.
4. When Windows asks, choose Replace the files in the destination.
5. Re-open/reload the project in Visual Studio.

IMPORTANT: CREATE THE DATABASE TABLES ONCE
Visual Studio > Tools > NuGet Package Manager > Package Manager Console
Make sure the School Management System project is the Default project, then run:

Add-Migration M01_IdentityUsersRoles
Update-Database

Then run the project. On first successful startup the roles are created automatically.

DEVELOPMENT LOGIN
Email: principal@school.local
Password: School@12345

This development seed is enabled only in appsettings.Development.json.
Before any real deployment, disable/remove SeedAdmin or change the credential.

IF YOU USE A DIFFERENT SQL SERVER
Edit ConnectionStrings:DefaultConnection in appsettings.json BEFORE Update-Database.
Current default is LocalDB:
Server=(localdb)\MSSQLLocalDB;Database=SchoolManagementSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True

MANUAL TEST CHECKLIST
1. Run app and open /Account/Login.
2. Login with principal@school.local / School@12345.
3. Confirm redirect to Principal Dashboard.
4. Open Users & Roles.
5. Create a Teacher account with a temporary password.
6. Logout and login as that Teacher.
7. Confirm Teacher dashboard opens and Users & Roles is not accessible.
8. Create another staff user and test role assignment.
9. Enter a wrong password 5 times and confirm lockout behavior.
10. Use Change Password and confirm the new password works.
11. Use Forgot Password in Development and open the displayed development reset link.
12. As Principal, edit a user and deactivate the account; confirm that account cannot log in.
13. Set a temporary password for a user; confirm the user is forced to change it after login.
14. Check dbo.AuditLogs in SQL Server and confirm M01 activity is recorded.

M01 COMPLETION NOTE
The production email/SMS delivery of password-reset links belongs to the later Communication module.
For M01, reset links are safely exposed only in Development, while authorized user managers can also set a temporary password.
