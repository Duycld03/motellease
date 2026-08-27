using System.Text.Json.Nodes;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Notifications.Contracts;

/// <summary>
/// One notification, rendered. The row stores keys and a payload (docs/erd.md §6), so the sentence
/// is built at read time in the reader's language; <see cref="Payload"/> is passed through as well
/// so a client can format the same values its own way — an amount with a currency symbol, a date in
/// the local timezone.
/// </summary>
public sealed record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    JsonNode? Payload,
    string? LinkUrl,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

/// <summary>What the bell badge needs, without pulling a page of rows to count them.</summary>
public sealed record UnreadNotificationCountResponse(int Unread);

public sealed record MarkNotificationsReadResponse(int Marked);
