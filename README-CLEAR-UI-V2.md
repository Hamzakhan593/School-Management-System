# Clear UI v2 — reference-based readability update

The school application has been revised to follow the supplied Master Feed Agency screenshot's visual hierarchy and readability. School workflows and English action names are retained.

## Visible changes

- Larger normal text: 20 px on wide desktop screens, 18 px on laptops and 17 px on tablets/mobile. Tables, labels and help text use readable sizes instead of miniature captions.
- A wider white sidebar with larger icons, generously spaced labels and a clear sage-green active item.
- A more distinct sage-grey page background, white cards, visible borders and restrained shadows.
- A pale sage dashboard introduction panel, prominent green actions and larger statistics.
- Larger form fields and buttons (normally at least 50 px tall), stronger text contrast and clearer field labels.
- Financial cards stay on two columns on narrower laptops and one column on phones so complete amounts remain readable.
- The design applies across existing forms, tables, tabs, dialogs, sign-in, reports, staff, fees, classes and attendance, not only the dashboard.
- Student summary cards are compact on mobile so search starts higher on the screen. Search guidance is visible text rather than a truncated placeholder.
- The offline theme and service-worker cache were updated to match.

## Verification

- 22 routes checked at 1920, 1440, 768 and 390 CSS pixels (88 checks). The final larger desktop text was rechecked on all 22 routes (110 checks total).
- No page-width overflow, clipped tested headings/buttons/statistics or JavaScript errors in these checks.
- Admission, student search, partial fee collection, receipt generation, duplicate-request protection and attendance save flows rerun successfully.
- Pagination, profile tabs, mobile navigation and 125% / 150% zoom-equivalent and CSS-zoom checks rerun.
- Desktop, laptop and mobile screenshots were visually inspected against the reference.
- Build passed with 0 errors; the same three pre-existing camera/integration warnings remain.

These checks used synthetic demo records in the isolated local QA database. Screenshots show the demo banner. Physical mobile devices, native browser-menu zoom, printers, biometric hardware and production integrations were not tested in this revision.

## Applying v2

This is the full updated project ZIP. If the previous UI update is already installed, this revision adds no new database migration and does not change financial or admission business rules.

The only application files changed since the previous delivered ZIP are:

1. `School Management System/Views/Shared/_Layout.cshtml`
2. `School Management System/Views/Students/Index.cshtml`
3. `School Management System/wwwroot/css/clarity.css` (new)
4. `School Management System/wwwroot/service-worker.js`
5. `School Management System/wwwroot/manifest.json`
6. `School Management System/wwwroot/offline.html`
7. `School Management System/Views/Dashboard/Index.cshtml`

You can copy these into the existing repository while preserving its `.git`, database configuration and uploads. Rebuild and restart the app. The new assets have versioned URLs and the service-worker cache version has changed.

For an installation that has not received the first UI update, follow `README-UI-UPDATE.md` and apply its payment-request migration as well. The original delivery report remains included as historical test information.

Open `Clear UI v2 Screenshots/index.html` for this revision's actual screenshots.
