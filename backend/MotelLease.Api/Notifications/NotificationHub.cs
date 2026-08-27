using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MotelLease.Application.Notifications;
using MotelLease.Application.Notifications.Contracts;
using MotelLease.Infrastructure.Security;

namespace MotelLease.Api.Notifications;

/// <summary>
/// The realtime channel behind <c>/hubs/notifications</c>. It has no methods: a client only
/// listens, and everything it could ask for is already an HTTP endpoint. Server pushes arrive as
/// <c>notification</c> with a <see cref="NotificationResponse"/> body.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub;

public static class NotificationRealtimeSetup
{
    public const string HubPath = "/hubs/notifications";

    /// <summary>The client-side method name a push is delivered to.</summary>
    public const string PushMethod = "notification";

    public static IServiceCollection AddRealtimeNotifications(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
        services.AddScoped<INotificationRealtime, SignalRNotificationRealtime>();

        return services;
    }
}

/// <summary>
/// Maps a connection to a user id. The default provider reads the NameIdentifier claim, which the
/// access token does not carry: inbound claim mapping is off, so the subject stays <c>sub</c>
/// (Api/Extensions/AuthenticationSetup.cs).
/// </summary>
internal sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}

internal sealed class SignalRNotificationRealtime(IHubContext<NotificationHub> hub)
    : INotificationRealtime
{
    public Task PushAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default) =>
        hub.Clients
            .User(userId.ToString())
            .SendAsync(NotificationRealtimeSetup.PushMethod, notification, cancellationToken);
}
