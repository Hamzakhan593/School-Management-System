using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class CommunicationDispatcher : ICommunicationDispatcher
{
    private readonly ApplicationDbContext _db;
    private readonly ISystemSettingsService _settings;

    public CommunicationDispatcher(ApplicationDbContext db, ISystemSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<CommunicationDispatchResult> DispatchAsync(
        int schoolId,
        Notice notice,
        CommunicationChannel channel,
        string? subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        var recipients = await EstimateRecipientCountAsync(schoolId, notice, cancellationToken);

        if (channel == CommunicationChannel.InApp)
        {
            return new CommunicationDispatchResult(
                CommunicationDeliveryStatus.Sent,
                recipients,
                "Built-in notice board",
                "Published in the School Management Software notice board. No third-party provider is required.");
        }

        var settings = await _settings.GetAsync(schoolId, cancellationToken);
        var (enabled, provider) = channel switch
        {
            CommunicationChannel.Email => (settings.EmailNotificationsEnabled, settings.EmailProviderName),
            CommunicationChannel.Sms => (settings.SmsNotificationsEnabled, settings.SmsProviderName),
            CommunicationChannel.WhatsApp => (settings.WhatsAppNotificationsEnabled, settings.WhatsAppProviderName),
            _ => (false, (string?)null)
        };

        if (!enabled)
            return new CommunicationDispatchResult(CommunicationDeliveryStatus.NotConfigured, recipients, "Disabled in Settings", $"{channel} delivery is disabled in Settings & Master Data.");

        return new CommunicationDispatchResult(
            CommunicationDeliveryStatus.NotConfigured,
            recipients,
            string.IsNullOrWhiteSpace(provider) ? "Provider not selected" : provider.Trim(),
            $"{channel} is enabled, but a real provider adapter/credential set is still required on the server. Secrets are intentionally not stored in the settings database.");
    }

    private async Task<int> EstimateRecipientCountAsync(int schoolId, Notice notice, CancellationToken cancellationToken)
    {
        if (notice.Audience == NoticeAudience.Staff)
            return await _db.Staff.CountAsync(x => x.SchoolId == schoolId && x.Status == StaffStatus.Active, cancellationToken);

        if (notice.Audience == NoticeAudience.Parents)
            return await _db.Guardians.CountAsync(x => x.SchoolId == schoolId, cancellationToken);

        if (notice.Audience == NoticeAudience.Students)
            return await _db.Students.CountAsync(x => x.SchoolId == schoolId && x.Status == StudentStatus.Active, cancellationToken);

        if (notice.Audience == NoticeAudience.Class && notice.SchoolClassId.HasValue)
        {
            var query = _db.StudentEnrollments.AsNoTracking().Where(x =>
                x.SchoolId == schoolId && x.IsCurrent && x.SchoolClassId == notice.SchoolClassId.Value);
            if (notice.SectionId.HasValue)
                query = query.Where(x => x.SectionId == notice.SectionId.Value);
            return await query.Select(x => x.StudentId).Distinct().CountAsync(cancellationToken);
        }

        var staff = await _db.Staff.CountAsync(x => x.SchoolId == schoolId && x.Status == StaffStatus.Active, cancellationToken);
        var students = await _db.Students.CountAsync(x => x.SchoolId == schoolId && x.Status == StudentStatus.Active, cancellationToken);
        return staff + students;
    }
}
