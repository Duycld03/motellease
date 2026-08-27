using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Deposits;

/// <summary>
/// Releases accepted requests whose payment deadline has passed (docs/domain-rules.md §2, §8). The
/// request becomes Expired and the room returns to Available, so an unpaid request cannot hold a
/// room indefinitely.
///
/// The rule lives here rather than in the background service so it can be run — and tested —
/// without a timer. The service in the Api layer only decides when.
/// </summary>
public sealed class ExpireOverdueDepositsHandler(
    IAppDbContext database,
    NotificationDispatcher notifications,
    TimeProvider time)
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();

        var due = await database.Deposits
            .Where(d => d.Status == DepositStatus.Accepted
                        && d.ExpiresAt != null
                        && d.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        foreach (var deposit in due)
        {
            deposit.Status = DepositStatus.Expired;

            await DepositRules.SyncRoomStatusAsync(database, deposit, cancellationToken);

            await DepositRules.NotifyTenantAsync(
                database,
                notifications,
                deposit,
                NotificationType.DepositExpired,
                cancellationToken);
        }

        await database.SaveChangesAsync(cancellationToken);

        await notifications.DeliverAsync(cancellationToken);

        return due.Count;
    }
}
