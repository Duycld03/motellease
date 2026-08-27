using System.Text.Json;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Notifications;

/// <summary>
/// Writes in-app notifications. The row carries i18n keys plus a payload, never a finished
/// sentence, so the recipient reads it in the language they have selected when they open it
/// rather than the one in force when it was sent (docs/domain-rules.md §7).
///
/// Nothing is saved here: the row joins the caller's own SaveChanges, so a notification about
/// something that failed to commit is never left behind. Realtime delivery over SignalR is a
/// separate concern and does not change what is stored.
/// </summary>
public sealed class NotificationDispatcher(IAppDbContext database)
{
    public void Queue(
        Guid userId,
        NotificationType type,
        object payload,
        string? linkUrl = null) =>
        database.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            // The enum value is the key prefix, so a new type needs no lookup table
            // (MotelLease.Domain.Enums.NotificationType).
            TitleKey = $"notification.{type}.title",
            BodyKey = $"notification.{type}.body",
            PayloadJson = JsonSerializer.Serialize(payload),
            LinkUrl = linkUrl
        });
}
