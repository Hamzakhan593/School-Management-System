using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Options;
using School_Management_System.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;

        // Baseline only. School-specific password rules are enforced by SchoolPasswordValidator (M19).
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "SchoolManagement.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(12); // M19 middleware enforces the school-specific inactivity timeout.
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IPasswordValidator<ApplicationUser>, SchoolPasswordValidator>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISchoolContextService, SchoolContextService>();
builder.Services.AddScoped<IAcademicSessionService, AcademicSessionService>();
builder.Services.AddScoped<IAdmissionFileService, AdmissionFileService>();
builder.Services.AddScoped<IAdmissionService, AdmissionService>();
builder.Services.AddScoped<IStudentFileService, StudentFileService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IStaffFileService, StaffFileService>();
builder.Services.AddScoped<IStaffAttendanceService, StaffAttendanceService>();
builder.Services.AddScoped<IStaffPayrollService, StaffPayrollService>();
builder.Services.AddScoped<IStaffPayrollPdfService, StaffPayrollPdfService>();
builder.Services.AddScoped<IExpenseFileService, ExpenseFileService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ICommunicationFileService, CommunicationFileService>();
builder.Services.AddScoped<ICommunicationDispatcher, CommunicationDispatcher>();
builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection("Backup"));
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddHostedService<BackupHostedService>();
builder.Services.Configure<AttendanceOptions>(builder.Configuration.GetSection("Attendance"));
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.Configure<AttendanceIntegrationOptions>(builder.Configuration.GetSection("AttendanceIntegrations"));
builder.Services.AddScoped<IAttendanceIntegrationService, AttendanceIntegrationService>();
builder.Services.AddScoped<ICameraRecognitionProvider, ManualOnlyCameraRecognitionProvider>();
builder.Services.Configure<FeeOptions>(builder.Configuration.GetSection("Fees"));
builder.Services.AddScoped<IFeeService, FeeService>();
builder.Services.AddScoped<IFeePdfService, FeePdfService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IResultPdfService, ResultPdfService>();
builder.Services.AddHostedService<MonthlyChallanGenerationHostedService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<DynamicSessionTimeoutMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await DbInitializer.InitializeAsync(app.Services, app.Configuration);
await DemoDataSeeder.SeedAsync(app.Services, app.Configuration);

app.Run();
