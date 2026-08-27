using MotelLease.Application.Notifications.Contracts;

namespace MotelLease.Application.Notifications;

/// <summary>
/// Realtime delivery of a notification that is already committed. Implemented over SignalR in the
/// Api layer (docs/api-design.md, <c>/hubs/notifications</c>); the stored row is the record of
/// truth, so a client that missed the push still finds it in GET /notifications.
/// </summary>
public interface INotificationRealtime
{
    Task PushAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default);
}
