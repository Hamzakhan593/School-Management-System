# School Management System — UI/UX delivery

Delivered: 26 September 2026

## Implemented

- A shared warm-grey, white and deep emerald design across the existing Razor application: navigation, cards, forms, tables, dialogs, status messages, tabs, sign-in and offline/install screens.
- Workflow-based, role-aware navigation. Administration tools are grouped; teachers see assigned classes. Existing controller authorization remains in place.
- A focused dashboard with active students, today's attendance, unmarked students, monthly collections, outstanding fees and separate due/overdue amounts. Amounts include periods or as-of dates. Decorative charts were removed.
- Student search by name, admission number, current roll number and guardian phone. Class, section and status filters, 20-record pagination, and return-to-list search/page memory.
- Admissions retain the original enquiry → approval → application → review → admission workflow. Required details are grouped; optional information is collapsed; class suggestions reduce typing. Validation retains input.
- Fee search shows explicit student candidates with admission number and class/section. Ledger-to-payment navigation retains the selected student. The payment screen separates payable, received and remaining amounts, explains partial payments, retains invalid input and shows a saved-payment page with Print Receipt.
- Payment retry protection uses a request ID, SQL transaction lock and unique database index. The same request returns its existing receipt.
- Attendance begins Unmarked for unrecorded days. Mark All Present requires a deliberate confirmation. Saving unmarked students is rejected, entered values are retained, and dependent sections load after class selection.
- Student profiles show real attendance history (latest 60 records) and published results to authorized management roles, plus guardian information and existing fee actions.
- Visible labels, keyboard focus, tab arrow-key navigation, searchable long selectors, mobile table cards, readable statuses and responsive controls.
- Demo configuration and demo-school registration markers display a visible demo banner. The supplied development demo remains available.

## Verification results

| Check | Result |
|---|---|
| .NET 9 / Razor build | Passed; 0 errors. Three pre-existing warnings are listed below. |
| Fresh isolated SQL Server schema | Original migrations plus the new payment-request migration applied successfully. |
| Admission | Enquiry saved, application validated, submitted, approved and permanent student profile opened. |
| Student search | Name, admission number, roll number and guardian phone searches passed. No-result state, pagination and returning to the previous search/page passed. |
| Fee collection | Partial-payment summary, save, confirmation and receipt PDF passed. Overpayment rejected with reference retained. |
| Duplicate payment | Two concurrent retries returned the same receipt. Database check confirmed one payment per request ID. |
| Attendance | Previously unrecorded day began entirely Unmarked. Server rejected unmarked input. Deliberate bulk marking, absent/leave changes, save and reload passed. |
| Student profile | Attendance/results records, selected-student fee links and keyboard tabs checked. |
| Roles | Teacher, accounts and reception navigation and permitted/denied server routes checked. |
| Screen coverage | 36 routes at 1440, 768 and 390 CSS pixels, plus 10 zoom-equivalent layout checks: 118 checks. No page overflow or JavaScript exceptions. |
| Accessibility follow-up | Missing class/settings labels fixed; final photo field label added and rechecked. Keyboard tabs and mobile drawer/Escape checked. |
| Zoom | 125% and 150% equivalent viewport checks, plus CSS zoom stress checks on admission/payment forms. |

Screenshots were captured from the real running application using synthetic data in `SchoolUIUX_QA_20260925`, a separate local SQL Server database. Desktop, tablet and mobile examples were visually inspected. The original input ZIP and the original configured school database were not modified.

## Limits and untested items

- Mobile/tablet checks used browser viewport emulation, not physical phones or tablets. Native browser-menu zoom, Windows display scaling, Safari and Firefox were not tested.
- Printer hardware, biometric/camera devices, SMS/email/WhatsApp providers, external payment gateways, production deployment, backup restoration and real production data were not exercised.
- Other modules received shared UI changes and route/layout smoke checks. Every business transaction in payroll, exams, reports, settings and integrations was not exhaustively retested.
- This is an updated source project, not a hosted deployment or a bundled SQL Server installation. Apply the included migration before using fee collection.
- Existing build warnings: one nullable-value warning in CameraAttendanceController and two model-binding naming warnings in AttendanceIntegrationsController. These unrelated implementation warnings were present before the redesign.

## Database change

`UI_PaymentRequestId` adds only a nullable `FeePayments.RequestId` column and a filtered unique index scoped to the school. Existing payment rows and balances are retained. No data reset, table replacement or automatic production migration was added.

See `README-UI-UPDATE.md` in the project ZIP for setup and upgrade instructions. `UI-PaymentRequestId.sql` is the optional idempotent SQL upgrade script for a database already at the supplied M19 migration.
