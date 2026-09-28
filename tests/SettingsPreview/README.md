# Settings UI checks

The preview renders the actual compiled Settings Razor view and navigation partial with sample values and a sample principal role. It loads the app’s complete CSS stack inside matching sidebar, header and content containers. It does not register a database connection, and rejects all POST requests. It does not test saving to a live school database.

From the repository root, start the preview in one terminal:

```powershell
dotnet run --project tests/SettingsPreview/SettingsPreview.csproj --urls http://localhost:5299
```

In another terminal, run the browser checks with Node.js, Playwright and Microsoft Edge installed:

```powershell
node tests/settings.ui.cjs
```

If Playwright is in a shared runtime, set `CHALLAN_NODE_MODULES` to that runtime's `node_modules` directory. `SETTINGS_PREVIEW_URL` can override the sample preview URL.

The checks cover every field's help text, search, unsaved changes, number and fee examples, validation across hidden categories, submitted form values, server-error redisplay, and all categories at 1920, 1440, 1280, 768 and 390 pixels. Layout checks also cover larger text, toggle text width, print-field alignment, save-bar placement, sidebar active states and the mobile navigation drawer. Screenshots are written to the temporary `school-settings-preview` folder.


Academic previews are available at `/preview?module=classes`, `workspace`, `marks`, `results`, and `card`. Run `node tests/academics.ui.cjs` for responsive layout and marks-entry interaction checks. `/sample-result.pdf` renders the production PDF service with demonstration data; `?bulk=1` generates two cards and `?long=1` exercises pagination.

Run the independent academic service tests with:

```powershell
dotnet run --project tests/Academics.Tests/Academics.Tests.csproj --artifacts-path "$env:TEMP/school-academics-tests"
```

These use an ephemeral SQLite database, never the school's SQL Server database. They exercise teacher/section access, partial drafts, submission, approval locks, missing marks, ties across sections, school ranking readiness, correction safeguards and unsectioned students. Use `--artifacts-path "$env:TEMP/school-academics-preview"` when running the preview alongside Visual Studio to avoid locking application binaries.

The result-card renderer uses PDFsharp/MigraDoc 6.2 and Windows Arial fonts on this Windows-hosted application. A non-Windows deployment needs a PDFsharp font resolver before generating PDFs. No database migration is required for these changes.

Exams and result workspace previews also support exams, examdetails, and classresult module names. Run node tests/results-workspace.ui.cjs to check five screen widths with normal and enlarged text, date wrapping, text encoding, search, mobile selection, and result links. These checks use demonstration data and do not submit changes or download live student records.
