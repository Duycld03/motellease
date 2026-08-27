using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Notifications.Contracts;
using MotelLease.Domain.Entities;

namespace MotelLease.Application.Notifications;

/// <summary>
/// Turns a stored row into a sentence. The template comes from the resource catalogue in the
/// reader's language and the values come from the row's own payload, so the same notification
/// reads differently to two people and differently again after one of them switches language
/// (docs/erd.md §6).
/// </summary>
internal static class NotificationText
{
    internal static NotificationResponse Render(
        Notification notification,
        ILocalizer localizer,
        string language)
    {
        var payload = JsonNode.Parse(notification.PayloadJson) as JsonObject;

        return new NotificationResponse(
            notification.Id,
            notification.Type,
            Fill(localizer.Get(notification.TitleKey, language), payload),
            Fill(localizer.Get(notification.BodyKey, language), payload),
            payload,
            notification.LinkUrl,
            notification.IsRead,
            notification.ReadAt,
            notification.CreatedAt);
    }

    /// <summary>
    /// Substitutes <c>{name}</c> placeholders from the payload. Named rather than positional
    /// because the two catalogues are translated independently and a Vietnamese sentence does not
    /// keep the English word order — the translator must be free to move the values around.
    /// An unknown placeholder is left as written, which keeps a stale template visible instead of
    /// producing a sentence with a hole in it.
    /// </summary>
    private static string Fill(string template, JsonObject? payload)
    {
        if (payload is null || !template.Contains('{', StringComparison.Ordinal))
        {
            return template;
        }

        var text = new StringBuilder(template.Length);
        var index = 0;

        while (index < template.Length)
        {
            var open = template.IndexOf('{', index);
            var close = open < 0 ? -1 : template.IndexOf('}', open + 1);

            if (close < 0)
            {
                text.Append(template, index, template.Length - index);

                break;
            }

            text.Append(template, index, open - index);

            var placeholder = template[open..(close + 1)];

            text.Append(Value(payload, template[(open + 1)..close]) ?? placeholder);

            index = close + 1;
        }

        return text.ToString();
    }

    private static string? Value(JsonObject payload, string name) =>
        payload.TryGetPropertyValue(name, out var node)
            ? node switch
            {
                null => string.Empty,
                JsonValue value => value.ToString(),
                _ => node.ToJsonString()
            }
            : null;
}

/// <summary>
/// GET /notifications. Own rows only, newest first, with <c>unreadOnly</c> for the bell dropdown
/// (docs/api-design.md). There is no role branch: a notification is addressed to one person.
/// </summary>
public sealed class ListNotificationsHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    ILocalizer localizer,
    IRequestContext requestContext)
{
    public async Task<PagedResponse<NotificationResponse>> HandleAsync(
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var query = database.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var rows = await Paged.FromAsync(
            query.OrderByDescending(n => n.CreatedAt),
            page,
            pageSize,
            cancellationToken);

        var language = requestContext.Language;

        return new PagedResponse<NotificationResponse>(
            [.. rows.Items.Select(n => NotificationText.Render(n, localizer, language))],
            rows.Page,
            rows.PageSize,
            rows.Total,
            rows.TotalPages);
    }
}

/// <summary>GET /notifications/unread-count.</summary>
public sealed class CountUnreadNotificationsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<UnreadNotificationCountResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        return new UnreadNotificationCountResponse(
            await database.Notifications.CountAsync(
                n => n.UserId == userId && !n.IsRead, cancellationToken));
    }
}

/// <summary>
/// PUT /notifications/{id}/read. Scoped to the caller's own rows, so somebody else's id is a 404
/// rather than a 403: answering "not yours" would confirm that the id exists.
/// </summary>
public sealed class MarkNotificationReadHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    ILocalizer localizer,
    IRequestContext requestContext,
    TimeProvider time)
{
    public async Task<NotificationResponse> HandleAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var notification = await database.Notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Notification.NotFound);

        // Idempotent: opening the same notification twice must not move the timestamp, which is
        // when it was first seen.
        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = time.GetUtcNow();

            await database.SaveChangesAsync(cancellationToken);
        }

        return NotificationText.Render(notification, localizer, requestContext.Language);
    }
}

/// <summary>
/// PUT /notifications/read-all. One UPDATE rather than a page of loaded rows: the whole point of
/// the endpoint is a user with a backlog.
/// </summary>
public sealed class MarkAllNotificationsReadHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<MarkNotificationsReadResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var now = time.GetUtcNow();

        var marked = await database.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now)
                    .SetProperty(n => n.UpdatedAt, now),
                cancellationToken);

        return new MarkNotificationsReadResponse(marked);
    }
}
