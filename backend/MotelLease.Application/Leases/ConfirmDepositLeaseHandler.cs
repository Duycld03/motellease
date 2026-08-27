using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Domain.Rooms;

namespace MotelLease.Application.Leases;

internal static class LeaseRules
{
    internal static async Task<LeaseResponse> LoadAsync(
        IAppDbContext database,
        Guid leaseId,
        CancellationToken cancellationToken) =>
        await database.Leases
            .AsNoTracking()
            .Where(l => l.Id == leaseId)
            .Select(l => new LeaseResponse(
                l.Id,
                l.RoomId,
                l.Room.RoomNumber,
                l.Room.BoardingHouseId,
                l.Room.BoardingHouse.Name,
                l.DepositId,
                l.PrimaryTenantUserId,
                l.PrimaryTenant.FullName,
                l.StartDate,
                l.EndDate,
                l.TermMonths,
                l.MonthlyRent,
                l.DepositHeld,
                l.Status,
                l.Tenants
                    .OrderByDescending(t => t.IsPrimary)
                    .Select(t => new LeaseTenantResponse(
                        t.Id,
                        t.UserId,
                        t.FullName,
                        t.PhoneNumber,
                        t.IsPrimary,
                        t.MovedInAt,
                        t.MovedOutAt))
                    .ToList(),
                l.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Lease.NotFound);
}

/// <summary>
/// POST /deposits/{id}/confirm-lease. Turns a paid deposit into the contract it was paying for: the
/// deposit is consumed (Completed), the money it represents becomes the deposit the lease holds, and
/// the room passes from Reserved to Occupied.
///
/// The frozen amount travels from the deposit onto the lease and is never re-read from the room type
/// — an owner raising the asking price after a tenant has paid must not change what that tenant
/// signed for (docs/domain-rules.md §3). One transaction, because a lease created without its primary
/// tenant would be a contract with nobody living under it, and the occupancy count is taken from
/// those rows (§9.2).
/// </summary>
public sealed class ConfirmDepositLeaseHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<LeaseResponse> HandleAsync(
        Guid depositId,
        CancellationToken cancellationToken = default)
    {
        var deposit = await database.Deposits.FirstOrDefaultAsync(
            d => d.Id == depositId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Deposit.NotFound);

        var room = await database.Rooms.FirstOrDefaultAsync(
            r => r.Id == deposit.RoomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        // Only a paid deposit becomes a contract. An accepted one has promised the room but not
        // funded it, and anything else has stopped holding it.
        if (deposit.Status != DepositStatus.Paid)
        {
            throw new BusinessRuleException(MessageKeys.Deposit.NotPaid);
        }

        // The partial unique index is the authority (§9.1); this check turns the race into a clear
        // conflict rather than a constraint violation.
        if (await database.Leases.AnyAsync(
                l => l.RoomId == room.Id && l.Status == LeaseStatus.Active, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Lease.RoomAlreadyLeased);
        }

        await EnsureRoomTakesOneMoreAsync(room, cancellationToken);

        var tenant = await database.Users
            .Where(u => u.Id == deposit.UserId)
            .Select(u => new { u.FullName, u.PhoneNumber })
            .FirstAsync(cancellationToken);

        await using var scope = await database.BeginTransactionAsync(cancellationToken);

        var lease = new Lease
        {
            RoomId = room.Id,
            DepositId = deposit.Id,
            PrimaryTenantUserId = deposit.UserId,
            StartDate = deposit.RequestedStartDate,
            EndDate = deposit.RequestedStartDate.AddMonths(deposit.RequestedTermMonths),
            TermMonths = deposit.RequestedTermMonths,
            MonthlyRent = deposit.Amount,
            DepositHeld = deposit.Amount,
            Status = LeaseStatus.Active,
            CreatedByUserId = currentUser.RequireUserId()
        };

        database.Leases.Add(lease);

        database.LeaseTenants.Add(new LeaseTenant
        {
            LeaseId = lease.Id,
            UserId = deposit.UserId,
            FullName = tenant.FullName,
            PhoneNumber = tenant.PhoneNumber,
            IsPrimary = true,
            MovedInAt = time.GetUtcNow()
        });

        // The deposit has done its job; the lease now holds the money it stood for.
        deposit.Status = DepositStatus.Completed;

        // Derived, not assigned per branch: an Active lease means Occupied (§9.3). The lease is not
        // saved yet, so the fact is passed in rather than queried for.
        room.Status = RoomStatusPolicy.DeriveFromCommitments(
            hasLiveLease: true, hasHoldingDeposit: false);

        await database.SaveChangesAsync(cancellationToken);

        await scope.CommitAsync(cancellationToken);

        return await LeaseRules.LoadAsync(database, lease.Id, cancellationToken);
    }

    /// <summary>
    /// Checks the occupancy cap before adding the primary tenant (§1, §9.2). It passes for an empty
    /// room by definition, and the check is here rather than assumed so a DormStyle room already at
    /// its cap cannot be given another contract.
    /// </summary>
    private async Task EnsureRoomTakesOneMoreAsync(Room room, CancellationToken cancellationToken)
    {
        var limits = await database.Rooms
            .Where(r => r.Id == room.Id)
            .Select(r => new
            {
                HouseType = r.BoardingHouse.Type,
                r.RoomType.MaxOccupants
            })
            .FirstAsync(cancellationToken);

        var living = await database.LeaseTenants.CountAsync(
            t => t.MovedOutAt == null
                 && database.Leases.Any(l =>
                     l.Id == t.LeaseId
                     && l.RoomId == room.Id
                     && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring)),
            cancellationToken);

        var cap = RoomOccupancyPolicy.MaxOccupants(limits.HouseType, limits.MaxOccupants);

        if (living + 1 > cap)
        {
            throw new ConflictException(MessageKeys.Lease.RoomFullyOccupied, cap);
        }
    }
}
