using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class MonthlyChallanGenerationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlyChallanGenerationHostedService> _logger;

    public MonthlyChallanGenerationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MonthlyChallanGenerationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunGenerationCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automatic monthly challan generation check failed.");
            }

            try { await Task.Delay(TimeSpan.FromHours(12), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunGenerationCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var feeService = scope.ServiceProvider.GetRequiredService<IFeeService>();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();

        var activeSessions = await db.AcademicSessions.AsNoTracking()
            .Where(x => x.Status == AcademicSessionStatus.Active)
            .Select(x => new { x.Id, x.SchoolId, x.StartDate, x.EndDate })
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            var settings = await settingsService.GetAsync(session.SchoolId, cancellationToken);
            if (!settings.AutoGenerateMonthlyChallans) continue;

            var today = GetLocalToday(settings.SchoolTimeZoneId);
            if (today < session.StartDate.Date || today > session.EndDate.Date) continue;
            if (today.Day < Math.Clamp(settings.MonthlyChallanGenerationDay, 1, DateTime.DaysInMonth(today.Year, today.Month))) continue;

            var period = new DateTime(today.Year, today.Month, 1);
            var request = new ChallanGenerationViewModel
            {
                AcademicSessionId = session.Id,
                BillingMonth = period,
                Scope = FeeBatchScope.WholeSchool,
                PreviewToken = $"AUTO-{session.SchoolId}-{period:yyyy-MM}"
            };

            try
            {
                var batch = await feeService.GenerateAsync(session.SchoolId, null, request, cancellationToken);
                _logger.LogInformation(
                    "Automatic challan batch {BatchId}: school {SchoolId}, period {Period}, generated {Generated}, skipped {Skipped}.",
                    batch.Id, session.SchoolId, batch.BillingPeriod, batch.GeneratedCount, batch.SkippedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automatic challan generation failed for school {SchoolId}.", session.SchoolId);
            }
        }
    }

    private static DateTime GetLocalToday(string? timeZoneId)
    {
        foreach (var id in new[] { timeZoneId, "Asia/Karachi", "Pakistan Standard Time", "UTC" }.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(id!)).Date; }
            catch { }
        }
        return DateTime.UtcNow.Date;
    }
}
