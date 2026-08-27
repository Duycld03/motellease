using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Bills;

public sealed class SweepBillRemindersHandler(
    IAppDbContext database,
    NotificationDispatcher notifications,
    TimeProvider time)
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        var targetDueSoonDate = today.AddDays(3);

        var issuedBills = await database.PaymentBills
            .Include(b => b.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(b => b.Lease)
            .Where(b => b.Status == BillStatus.Issued && b.DueDate.HasValue)
            .ToListAsync(cancellationToken);

        var count = 0;

        foreach (var bill in issuedBills)
        {
            if (bill.DueDate!.Value < today)
            {
                bill.Status = BillStatus.Overdue;
                count++;

                notifications.Queue(
                    bill.Lease.PrimaryTenantUserId,
                    NotificationType.BillOverdue,
                    new
                    {
                        month = bill.Month,
                        year = bill.Year,
                        roomNumber = bill.Room.RoomNumber,
                        boardingHouseName = bill.Room.BoardingHouse.Name
                    },
                    linkUrl: $"/bills/{bill.Id}");

                notifications.Queue(
                    bill.Room.BoardingHouse.OwnerUserId,
                    NotificationType.BillOverdue,
                    new
                    {
                        month = bill.Month,
                        year = bill.Year,
                        roomNumber = bill.Room.RoomNumber,
                        boardingHouseName = bill.Room.BoardingHouse.Name
                    },
                    linkUrl: $"/bills/{bill.Id}");
            }
            else if (bill.DueDate!.Value == targetDueSoonDate)
            {
                notifications.Queue(
                    bill.Lease.PrimaryTenantUserId,
                    NotificationType.BillDueSoon,
                    new
                    {
                        month = bill.Month,
                        year = bill.Year,
                        roomNumber = bill.Room.RoomNumber,
                        boardingHouseName = bill.Room.BoardingHouse.Name,
                        dueDate = bill.DueDate.Value.ToString("yyyy-MM-dd")
                    },
                    linkUrl: $"/bills/{bill.Id}");
                count++;
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
