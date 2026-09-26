using School_Management_System.Models;

namespace School_Management_System.Services;

public interface ICommunicationDispatcher
{
    Task<CommunicationDispatchResult> DispatchAsync(
        int schoolId,
        Notice notice,
        CommunicationChannel channel,
        string? subject,
        string message,
        CancellationToken cancellationToken = default);
}

public record CommunicationDispatchResult(
    CommunicationDeliveryStatus Status,
    int RecipientCount,
    string ProviderName,
    string Detail);
