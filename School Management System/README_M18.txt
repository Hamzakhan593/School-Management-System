M18 — PWA / MOBILE INSTALL & OFFLINE SUPPORT
School Management Software — Master Development Blueprint
============================================================

PURPOSE
-------
This patch adds the Progressive Web App/mobile layer to the existing M01–M17 application. It does NOT create a second Android/iOS codebase. The same ASP.NET Core MVC system becomes installable from supported browsers and stays responsive on phone, tablet and desktop.

INSTALLATION
------------
1. Extract this ZIP into the INNER project folder:
   School Management System\School Management System\
2. Choose "Replace files in destination" when Windows asks.
3. Clean Solution.
4. Rebuild Solution.
5. Run the project.

DATABASE / MIGRATION
--------------------
M18 adds NO database tables or columns.
Do NOT run Add-Migration for M18.
Do NOT run Update-Database specifically for M18.

FILES ADDED / CHANGED
---------------------
Controllers\MobileAppController.cs
Views\MobileApp\Index.cshtml
Views\Shared\_Layout.cshtml
Views\Shared\_AppNavigation.cshtml
Views\Shared\_SvgSprite.cshtml
wwwroot\manifest.json
wwwroot\service-worker.js
wwwroot\offline.html
wwwroot\js\pwa.js
wwwroot\css\pwa.css
wwwroot\icons\icon-192.png
wwwroot\icons\icon-512.png
wwwroot\icons\icon-maskable-512.png
wwwroot\icons\apple-touch-icon.png

WHAT M18 IMPLEMENTS
-------------------
- Web App Manifest with school-system name, theme and application icons.
- Service worker registered at root scope.
- Home-screen / desktop "Install App" support where the browser exposes the install event.
- Manual install guidance for iPhone/iPad and browsers that hide the install prompt.
- Professional "Mobile App" screen with device/PWA diagnostics.
- Online/offline connection indicator in the authenticated top bar.
- Clear offline banner when the connection is lost.
- Offline fallback page when navigation cannot reach the server.
- Static app-shell caching only; authenticated business HTML/data is intentionally NOT cached.
- Non-GET form submissions are blocked while offline in the browser UI.
- No background sync queue for payments, attendance posting, result publishing, payroll posting, promotions, backup/restore or other writes.
- Existing responsive M14 sidebar/mobile drawer is retained and used by the installed PWA.
- Existing M07 camera workflow remains the authorized camera attendance route.

SECURITY / DATA-SAFETY RULE
---------------------------
M18 does NOT cache authenticated student, fee, payroll, result or other business pages for offline use. This is intentional. The service worker caches only static application assets and a generic offline page. Live records remain server-controlled.

All write forms require a live connection. Financial posting, final attendance submission, result publication, payroll posting, annual promotion and restore are never silently queued or shown as successful offline.

PRODUCTION REQUIREMENTS
-----------------------
- Host the production system with HTTPS.
- A service worker requires HTTPS except on localhost during development.
- If the app is hosted under a virtual directory rather than the domain root, update manifest start_url/scope and service-worker registration paths accordingly.
- Keep the manifest/service-worker files publicly readable as static assets; school data/controllers remain protected by Identity authorization.

MANUAL TEST CHECKLIST
---------------------
A. DESKTOP / PWA
[ ] Login and open Mobile App from the left navigation.
[ ] Device diagnostics show Secure Context and Service Worker appropriately.
[ ] In Chrome/Edge on HTTPS or localhost, confirm an install option appears when browser criteria are met.
[ ] Install the app and confirm it launches in standalone/app-style mode.
[ ] Confirm the school dashboard, sidebar and role-based navigation still work.

B. MOBILE RESPONSIVENESS
[ ] Open on Android phone width.
[ ] Confirm desktop sidebar is replaced by the mobile drawer.
[ ] Open/close drawer and navigate between Dashboard, Attendance and Fees.
[ ] Confirm forms/tables remain usable at phone width.
[ ] Confirm the Mobile App page does not overflow horizontally.

C. OFFLINE SAFETY
[ ] While logged in, disconnect network.
[ ] Confirm the top bar shows Offline and an orange offline banner.
[ ] Navigate to a page that is not already available from server and confirm the generic offline screen appears.
[ ] Try submitting a POST form while offline; confirm the app blocks it and clearly says the change was NOT submitted/queued.
[ ] Reconnect and confirm the Online indicator returns.

D. CAMERA
[ ] On HTTPS/localhost, open Camera Attendance from the Mobile App page.
[ ] Confirm the browser can request camera permission in the existing M07 workflow.
[ ] Deny camera once and confirm the existing manual/fallback attendance path remains available.

E. REGRESSION
[ ] Login/logout works.
[ ] Dashboard works.
[ ] Fee payment/challan posting still requires server response.
[ ] Exam result publishing still requires server response.
[ ] Payroll posting still requires server response.
[ ] Annual promotion still requires server response.
[ ] Backup/restore screens still work online.

IMPORTANT NOTE ABOUT INSTALL BUTTONS
------------------------------------
Chrome/Edge only fire beforeinstallprompt when their PWA installability requirements are satisfied. If the install button is hidden, use the Mobile App page diagnostics and confirm HTTPS, manifest and service worker are active. iOS Safari uses Share -> Add to Home Screen instead of the Chromium install prompt.

BUILD VERIFICATION
------------------
This environment does not contain the .NET 9 SDK, so dotnet build could not be run here. After copying M18, use Visual Studio:
  Clean Solution -> Rebuild Solution -> Run
and complete the checklist above.
