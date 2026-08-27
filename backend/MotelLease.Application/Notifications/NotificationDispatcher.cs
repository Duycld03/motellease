using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Notifications;

/// <summary>
/// Writes in-app notifications. The row carries i18n keys plus a payload, never a finished
/// sentence, so the recipient reads it in the language they have selected when they open it
/// rather than the one in force when it was sent (docs/domain-rules.md §7).
///
/// Nothing is saved here: the row joins the caller's own SaveChanges, so a notification about
/// something that failed to commit is never left behind. Realtime delivery is the separate
/// <see cref="DeliverAsync"/> step for the same reason — it must not push a row that rolled back.
/// </summary>
public sealed class NotificationDispatcher(
    IAppDbContext database,
    INotificationRealtime realtime,
    ILocalizer localizer)
{
    private readonly List<Notification> _queued = [];

    public void Queue(
        Guid userId,
        NotificationType type,
        object payload,
        string? linkUrl = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            // The enum value is the key prefix, so a new type needs no lookup table
            // (MotelLease.Domain.Enums.NotificationType).
            TitleKey = $"notification.{type}.title",
            BodyKey = $"notification.{type}.body",
            PayloadJson = JsonSerializer.Serialize(payload),
            LinkUrl = linkUrl
        };

        database.Notifications.Add(notification);
        _queued.Add(notification);
    }

    /// <summary>
    /// Pushes everything queued so far to whoever is connected. Called after the caller's
    /// SaveChanges has succeeded.
    ///
    /// The sentence is rendered in each recipient's own stored language rather than the language of
    /// the request that triggered it: the person being told is not the person acting, and an owner
    /// working in English must not force English onto a Vietnamese tenant (docs/features.md §3.12).
    /// </summary>
    public async Task DeliverAsync(CancellationToken cancellationToken = default)
    {
        if (_queued.Count == 0)
        {
            return;
        }

        var recipients = _queued.Select(n => n.UserId).Distinct().ToList();

        var languages = await database.Users
            .Where(u => recipients.Contains(u.Id))
            .Select(u => new { u.Id, u.PreferredLanguage })
            .ToDictionaryAsync(u => u.Id, u => u.PreferredLanguage, cancellationToken);

        foreach (var notification in _queued)
        {
            var language = languages.GetValueOrDefault(
                notification.UserId, SupportedLanguages.Default);

            await realtime.PushAsync(
                notification.UserId,
                NotificationText.Render(notification, localizer, language),
                cancellationToken);
        }

        _queued.Clear();
    }
}
