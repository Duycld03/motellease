using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Leases;

public sealed class SweepLeaseExpiryHandler(
    IAppDbContext database,
    NotificationDispatcher notifications,
    TimeProvider time)
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        var thirtyDaysAhead = today.AddDays(30);

        var leases = await database.Leases
            .Include(l => l.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(l => l.Tenants)
            .Where(l => l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring)
            .ToListAsync(cancellationToken);

        var count = 0;

        foreach (var lease in leases)
        {
            if (lease.EndDate < today)
            {
                lease.Status = LeaseStatus.Ended;
                lease.EndedAt = now;

                foreach (var t in lease.Tenants.Where(t => t.MovedOutAt == null))
                {
                    t.MovedOutAt = now;
                }

                var hasOtherActiveLease = await database.Leases.AnyAsync(
                    l => l.RoomId == lease.RoomId && l.Id != lease.Id && l.Status == LeaseStatus.Active,
                    cancellationToken);

                var hasActiveDeposit = await database.Deposits.AnyAsync(
                    d => d.RoomId == lease.RoomId &&
                         (d.Status == DepositStatus.Accepted || d.Status == DepositStatus.Paid),
                    cancellationToken);

                if (!hasOtherActiveLease && !hasActiveDeposit && lease.Room.Status != RoomStatus.Maintenance)
                {
                    lease.Room.Status = RoomStatus.Available;
                }

                count++;
            }
            else if (lease.Status == LeaseStatus.Active && lease.EndDate <= thirtyDaysAhead)
            {
                lease.Status = LeaseStatus.Expiring;
                count++;

                notifications.Queue(
                    lease.PrimaryTenantUserId,
                    NotificationType.LeaseExpiring,
                    new
                    {
                        roomNumber = lease.Room.RoomNumber,
                        boardingHouseName = lease.Room.BoardingHouse.Name,
                        endDate = lease.EndDate.ToString("yyyy-MM-dd")
                    },
                    linkUrl: $"/leases/{lease.Id}");
            }
        }

        if (count > 0)
        {
            await database.SaveChangesAsync(cancellationToken);
            await notifications.DeliverAsync(cancellationToken);
        }

        return count;
    }
}
