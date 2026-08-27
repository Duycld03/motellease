using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

/// <summary>
/// Stores i18n keys plus parameters, never a rendered sentence: an old notification then
/// follows the reader's current language (docs/erd.md §6).
/// </summary>
public class Notification : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public string TitleKey { get; set; } = null!;
    public string BodyKey { get; set; } = null!;

    /// <summary>jsonb map of placeholder values (room number, amount, …).</summary>
    public string PayloadJson { get; set; } = "{}";

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

/// <summary>
/// Append-only. Deliberately not an <see cref="Entity"/>: there is no UpdatedAt because a
/// row is never modified or deleted (docs/erd.md §6).
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ActorUserId { get; set; }

    /// <summary>Dotted action name, e.g. <c>Account.Lock</c>, <c>Withdraw.Approve</c>.</summary>
    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;
    public Guid? EntityId { get; set; }

    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
