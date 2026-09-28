using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using School_Management_System.ViewModels;
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, WebRootPath = Environment.GetEnvironmentVariable("SETTINGS_WEBROOT") ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../School Management System/wwwroot")) });
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SettingsPageViewModel).Assembly);
builder.Services.AddSingleton<School_Management_System.Services.ISystemSettingsService, PreviewSettings>();
var app = builder.Build();
app.Use(async (context, next) => { if (context.Request.Method != "GET") { context.Response.StatusCode = 405; return; } await next(); });
app.UseStaticFiles();
app.MapGet("/sample-result.pdf", async (HttpContext http, IWebHostEnvironment env, School_Management_System.Services.ISystemSettingsService settings) => Results.File(await new School_Management_System.Services.ResultPdfService(settings, env).CreateBulkResultCardsPdfAsync(http.Request.Query.ContainsKey("bulk") ? [AcademicsSamples.Card(), AcademicsSamples.Card()] : [AcademicsSamples.Card(http.Request.Query.ContainsKey("long"))]), "application/pdf"));
app.MapGet("/preview", async (HttpContext http, ICompositeViewEngine engine, IModelMetadataProvider metadata, ITempDataProvider tempData) => {
 http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Preview principal"), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Principal") }, "Preview"));
 var action = new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
 var sample = http.Request.Query["module"].ToString();
 var module = sample switch { "classes" => "AcademicStructure", "marks" or "workspace" or "exams" or "examdetails" => "Exams", "results" or "card" or "classresult" => "Results", "fees" => "Fees", "attendance" => "Attendance", _ => "Settings" };
 var viewName = sample switch { "examdetails" => "Details", "classresult" => "ClassResult", "marks" => "Marks", "workspace" => "Workspace", "card" => "ResultCard", _ => "Index" };
 action.RouteData.Values["controller"] = module; action.RouteData.Values["action"] = viewName;
 var model = new SettingsPageViewModel { SchoolName = "School settings preview", ActiveSessionName = "2026–2027", FeeHeadCount = 5, GradingRuleCount = 6, SelectedSection = http.Request.Query["tab"].FirstOrDefault() ?? "numbering" };
 if (http.Request.Query.ContainsKey("error")) action.ModelState.AddModelError("SessionTimeoutMinutes", "Choose between 5 and 720 minutes.");
 object displayModel = module switch {
  "Fees" => new FeesDashboardViewModel { BillingPeriod = "2026-09", CurrentMonthCharges = 667000m, CurrentMonthCollected = 477175m, TotalOutstanding = 643850m, OverdueStudentCount = 98, ChallansThisMonth = 120 },
  "Attendance" => new AttendanceMarkingViewModel { IsManager = true, AcademicSessionId = 1, SchoolClassId = 1, SectionId = 1 },
  _ => module == "Settings" ? model : AcademicsSamples.Model(sample)
 };
 var data = new ViewDataDictionary(metadata, action.ModelState) { Model = displayModel };
 var result = engine.GetView(null, $"/Views/{module}/{viewName}.cshtml", false);
 using var writer = new StringWriter();
 var context = new ViewContext(action, result.View!, data, new TempDataDictionary(http, tempData), writer, new HtmlHelperOptions());
 await result.View!.RenderAsync(context);
 async Task<string> RenderPartial(string view) {
  var partial = engine.GetView(null, view, false);
  using var output = new StringWriter();
  await partial.View!.RenderAsync(new ViewContext(action, partial.View, data, new TempDataDictionary(http, tempData), output, new HtmlHelperOptions()));
  return output.ToString();
 }
 var navigation = await RenderPartial("/Views/Shared/_AppNavigation.cshtml");
 var icons = await RenderPartial("/Views/Shared/_SvgSprite.cshtml");
 var styles = new[] { "/lib/bootstrap/dist/css/bootstrap.min.css", "/css/site.css", "/css/pwa.css", "/css/workspace.css", "/css/clarity.css", "/css/navigation.css" };
 var head = string.Join("", styles.Select(href => $"<link rel='stylesheet' href='{href}'>"));
 return Results.Content("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>" + head + "</head><body class='sms-app-body'>" + icons + "<aside class='sms-sidebar d-none d-lg-flex'><a class='sms-brand'><span class='sms-brand-mark'>ST</span><span class='sms-brand-copy'><strong>The School of Thoughts</strong><small>School Management System</small></span></a>" + navigation + "</aside><div class='offcanvas offcanvas-start sms-mobile-drawer' id='smsMobileNav' tabindex='-1'><div class='offcanvas-header'><strong>School navigation</strong><button class='btn-close' data-bs-dismiss='offcanvas' aria-label='Close'></button></div><div class='offcanvas-body p-0'>" + navigation + "</div></div><div class='sms-app-shell'><header class='sms-topbar'><div class='d-flex align-items-center gap-3'><button class='sms-icon-btn d-lg-none' data-bs-toggle='offcanvas' data-bs-target='#smsMobileNav' aria-label='Open navigation'>☰</button><div><div class='sms-topbar-title'>" + System.Net.WebUtility.HtmlEncode(data["Title"]?.ToString() ?? module) + "</div><div class='sms-topbar-subtitle d-none d-sm-block'>The School of Thoughts</div></div></div><span class='d-none d-md-block'>School Principal</span></header><main class='sms-main-content' data-module='" + module + "'>" + writer + "</main></div><script src='/lib/jquery/dist/jquery.min.js'></script><script src='/lib/bootstrap/dist/js/bootstrap.bundle.min.js'></script><script src='/js/site.js'></script><script src='/lib/jquery-validation/dist/jquery.validate.min.js'></script><script src='/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js'></script><script src='/js/settings.js'></script><script src='/js/attendance-marking.js'></script><script src='/js/marks-entry.js'></script><script src='/js/academic-structure.js'></script><script src='/js/results-workspace.js'></script></body></html>", "text/html");
});
app.MapControllerRoute("default", "{controller=Settings}/{action=Index}/{id?}");
app.Run();

