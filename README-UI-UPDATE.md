# School Management System — updated UI

This is the original ASP.NET Core MVC / EF Core SQL Server project with the implemented UI/UX update. Open `School Management System.sln` in Visual Studio, or use the .NET CLI.

## Requirements

- .NET 9 SDK and SQL Server (or SQL Server LocalDB).
- The database connection is `ConnectionStrings:DefaultConnection` in the application configuration. Keep production credentials in environment variables or user secrets.

## Upgrade an existing installation

1. Keep your database backup and existing deployment configuration.
2. Restore/build the solution using .NET 9.
3. Apply the new `UI_PaymentRequestId` migration using EF Core 9. Existing records remain in place. The optional `UI-PaymentRequestId.sql` upgrades a database already at the supplied M19 migration.
4. Run the application with your existing SQL Server connection and environment. The new assets are versioned; the service-worker cache version was also updated.

From the solution directory (PowerShell):

```powershell
dotnet restore
dotnet build
# If dotnet-ef is not already installed:
dotnet tool install --global dotnet-ef --version 9.0.0
dotnet ef database update --project '.\School Management System\School Management System.csproj'
dotnet run --project '.\School Management System\School Management System.csproj'
```

Use a 9.x dotnet-ef version if it is already installed. The normal launch profiles use http://localhost:5205 and https://localhost:7094.

## Separate demo / evaluation database

The supplied Development configuration enables demo seeding and the seeded principal account. Use a separate empty evaluation database for a demo; do not point demo seeding at school records. An initial demo seed may take several minutes.

```powershell
$env:ConnectionStrings__DefaultConnection='Server=.;Database=SchoolManagementUiDemo;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true'
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef database update --project '.\School Management System\School Management System.csproj'
dotnet run --project '.\School Management System\School Management System.csproj'
```

The existing demo principal is `principal@school.local` / `School@12345`. These are development demo credentials only. Other demo accounts are documented in the original `README_DEMO_DATA.txt`.

For a real deployment, use the Production environment, keep `DemoData:Enabled=false` and `SeedAdmin:Enabled=false`, and retain the school's existing authentication configuration. Existing demo-school registration markers continue to show a demo label even when seeding is disabled.

## What is included

- Updated source, original solution and dependencies.
- Shared design stylesheet `wwwroot/css/workspace.css` and reusable behavior in `wwwroot/js/site.js`.
- Updated Razor views, the workflow changes, EF migration and SQL upgrade script.
- Delivery/test report and representative screenshots.

Build output, IDE caches, Git history, test cookies and generated QA backups are intentionally excluded. The original source ZIP is unchanged. This ZIP contains no production database export.

## Verification

Read `UI-UX-Delivery-Report.md` for actual passed checks and limits. Open `Clear UI v2 Screenshots/index.html` to browse real application screenshots with clearly labelled synthetic demo data.
