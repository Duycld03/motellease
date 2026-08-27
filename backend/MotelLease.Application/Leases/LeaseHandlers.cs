using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Domain.Rooms;

namespace MotelLease.Application.Leases;

internal static class LeaseRules
{
    internal static IQueryable<LeaseResponse> Project(IQueryable<Lease> query) =>
        query.Select(l => new LeaseResponse(
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
                .ThenBy(t => t.MovedInAt)
                .Select(t => new LeaseTenantResponse(
                    t.Id,
                    t.UserId,
                    t.FullName,
                    t.PhoneNumber,
                    t.IdCardNumber,
                    t.IsPrimary,
                    t.MovedInAt,
                    t.MovedOutAt))
                .ToList(),
            l.CreatedAt,
            l.EndedAt,
            l.EndReason,
            l.FinalElectricityReading,
            l.FinalWaterReading,
            l.DepositDeducted,
            l.DepositRefunded));

    internal static async Task<LeaseResponse> LoadAsync(
        IAppDbContext database,
        Guid leaseId,
        CancellationToken cancellationToken) =>
        await Project(database.Leases.AsNoTracking().Where(l => l.Id == leaseId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Lease.NotFound);
}

public sealed class ListLeasesHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<LeaseResponse>> HandleAsync(
        LeaseStatus? status = null,
        Guid? roomId = null,
        Guid? boardingHouseId = null,
        int page = 1,
        int pageSize = Paged.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var query = database.Leases.AsNoTracking().AsQueryable();

        if (currentUser.Role == UserRole.Tenant)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(l =>
                l.PrimaryTenantUserId == userId ||
                l.Tenants.Any(t => t.UserId == userId));
        }
        else if (boardingHouseId.HasValue)
        {
            await access.RequireStaffOrOwnerAsync(boardingHouseId.Value, cancellationToken);
            query = query.Where(l => l.Room.BoardingHouseId == boardingHouseId.Value);
        }
        else
        {
            var managedHouseIds = access.Managed().Select(b => b.Id);
            query = query.Where(l => managedHouseIds.Contains(l.Room.BoardingHouseId));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(l => l.RoomId == roomId.Value);
        }

        return await Paged.FromAsync(
            LeaseRules.Project(query.OrderByDescending(l => l.CreatedAt)),
            page,
            pageSize,
            cancellationToken);
    }
}

public sealed class GetLeaseHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<LeaseResponse> HandleAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        var lease = await database.Leases
            .AsNoTracking()
            .Where(l => l.Id == leaseId)
            .Select(l => new
            {
                l.Room.BoardingHouseId,
                l.PrimaryTenantUserId,
                TenantUserIds = l.Tenants.Where(t => t.UserId.HasValue).Select(t => t.UserId!.Value).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Lease.NotFound);

        if (currentUser.Role == UserRole.Tenant)
        {
            var userId = currentUser.RequireUserId();
            if (lease.PrimaryTenantUserId != userId && !lease.TenantUserIds.Contains(userId))
            {
                throw new ForbiddenException(MessageKeys.Lease.NotYours);
            }
        }
        else
        {
            await access.RequireStaffOrOwnerAsync(lease.BoardingHouseId, cancellationToken);
        }

        return await LeaseRules.LoadAsync(database, leaseId, cancellationToken);
    }
}

public sealed class GetCurrentLeaseHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<LeaseResponse?> HandleAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var activeLeaseId = await database.Leases
            .AsNoTracking()
            .Where(l => (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring) &&
                        (l.PrimaryTenantUserId == userId || l.Tenants.Any(t => t.UserId == userId && t.MovedOutAt == null)))
            .OrderByDescending(l => l.StartDate)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeLeaseId == null)
        {
            return null;
        }

        return await LeaseRules.LoadAsync(database, activeLeaseId.Value, cancellationToken);
    }
}

public sealed class GetRoomLeaseHistoryHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<IReadOnlyList<LeaseResponse>> HandleAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await database.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        return await LeaseRules.Project(
            database.Leases.AsNoTracking()
                .Where(l => l.RoomId == roomId)
                .OrderByDescending(l => l.StartDate))
            .ToListAsync(cancellationToken);
    }
}

public sealed class AddLeaseTenantHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    TimeProvider time)
{
    public async Task<LeaseResponse> HandleAsync(
        Guid leaseId,
        AddLeaseTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var lease = await database.Leases
            .Include(l => l.Room)
                .ThenInclude(r => r.BoardingHouse)
            .Include(l => l.Room)
                .ThenInclude(r => r.RoomType)
            .Include(l => l.Tenants)
            .FirstOrDefaultAsync(l => l.Id == leaseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Lease.NotFound);

        await access.RequireStaffOrOwnerAsync(lease.Room.BoardingHouseId, cancellationToken);

        if (lease.Status != LeaseStatus.Active && lease.Status != LeaseStatus.Expiring)
        {
            throw new BusinessRuleException(MessageKeys.Lease.NotActive);
        }

        var liveCount = lease.Tenants.Count(t => t.MovedOutAt == null);
        var maxCap = RoomOccupancyPolicy.MaxOccupants(
            lease.Room.BoardingHouse.Type,
            lease.Room.RoomType.MaxOccupants);

        if (liveCount + 1 > maxCap)
        {
            throw new ConflictException(
                MessageKeys.Lease.RoomFullyOccupied,
                maxCap);
        }

        var now = time.GetUtcNow();
        var newTenant = new LeaseTenant
        {
            LeaseId = lease.Id,
            UserId = request.UserId,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IdCardNumber = request.IdCardNumber?.Trim(),
            IsPrimary = false,
            MovedInAt = now,
            MovedOutAt = null
        };

        database.LeaseTenants.Add(newTenant);
        await database.SaveChangesAsync(cancellationToken);

        return await LeaseRules.LoadAsync(database, lease.Id, cancellationToken);
    }
}

public sealed class RemoveLeaseTenantHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    TimeProvider time)
{
    public async Task<LeaseResponse> HandleAsync(
        Guid leaseId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var lease = await database.Leases
            .Include(l => l.Room)
            .Include(l => l.Tenants)
            .FirstOrDefaultAsync(l => l.Id == leaseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Lease.NotFound);

        await access.RequireStaffOrOwnerAsync(lease.Room.BoardingHouseId, cancellationToken);

        var tenant = lease.Tenants.FirstOrDefault(t => t.Id == tenantId)
            ?? throw new NotFoundException(MessageKeys.Lease.TenantNotFound);

        if (tenant.IsPrimary)
        {
            throw new BusinessRuleException(MessageKeys.Lease.CannotRemovePrimaryTenant);
        }

        if (tenant.MovedOutAt.HasValue)
        {
            throw new BusinessRuleException(MessageKeys.Lease.TenantAlreadyMovedOut);
        }

        tenant.MovedOutAt = time.GetUtcNow();
        await database.SaveChangesAsync(cancellationToken);

        return await LeaseRules.LoadAsync(database, lease.Id, cancellationToken);
    }
}

public sealed class PreviewLeaseTerminationHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<LeaseTerminationPreviewResponse> HandleAsync(
        Guid leaseId,
        decimal finalElectricityReading,
        decimal finalWaterReading,
        decimal depositDeducted,
        CancellationToken cancellationToken = default)
    {
        var lease = await database.Leases
            .AsNoTracking()
            .Include(l => l.Room)
            .ThenInclude(r => r.BoardingHouse)
            .FirstOrDefaultAsync(l => l.Id == leaseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Lease.NotFound);

        if (currentUser.Role == UserRole.Tenant)
        {
            if (lease.PrimaryTenantUserId != currentUser.RequireUserId())
            {
                throw new ForbiddenException(MessageKeys.Lease.NotYours);
            }
        }
        else
        {
            await access.RequireStaffOrOwnerAsync(lease.Room.BoardingHouseId, cancellationToken);
        }

        var room = lease.Room;
        var house = room.BoardingHouse;

        var eleOld = room.CurrentElectricityReading;
        var waterOld = room.CurrentWaterReading;

        var eleQty = Math.Max(0, finalElectricityReading - eleOld);
        var waterQty = Math.Max(0, finalWaterReading - waterOld);

        var eleAmount = eleQty * house.ElectricityUnitPrice;
        var waterAmount = waterQty * house.WaterUnitPrice;

        var depositRefunded = Math.Max(0, lease.DepositHeld - depositDeducted - eleAmount - waterAmount);

        return new LeaseTerminationPreviewResponse(
            lease.Id,
            room.Id,
            lease.DepositHeld,
            eleOld,
            finalElectricityReading,
            eleQty,
            house.ElectricityUnitPrice,
            eleAmount,
            waterOld,
            finalWaterReading,
            waterQty,
            house.WaterUnitPrice,
            waterAmount,
            depositDeducted,
            depositRefunded);
    }
}

public sealed class TerminateLeaseHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    TimeProvider time)
{
    public async Task<LeaseResponse> HandleAsync(
        Guid leaseId,
        TerminateLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var lease = await database.Leases
            .Include(l => l.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(l => l.Tenants)
            .FirstOrDefaultAsync(l => l.Id == leaseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Lease.NotFound);

        await access.RequireStaffOrOwnerAsync(lease.Room.BoardingHouseId, cancellationToken);

        if (lease.Status != LeaseStatus.Active && lease.Status != LeaseStatus.Expiring)
        {
            throw new BusinessRuleException(MessageKeys.Lease.NotTerminable);
        }

        var room = lease.Room;
        var house = room.BoardingHouse;

        if (request.FinalElectricityReading < room.CurrentElectricityReading ||
            request.FinalWaterReading < room.CurrentWaterReading)
        {
            throw new BusinessRuleException(MessageKeys.Lease.ReadingBelowCurrent);
        }

        var eleQty = request.FinalElectricityReading - room.CurrentElectricityReading;
        var waterQty = request.FinalWaterReading - room.CurrentWaterReading;

        var eleAmount = eleQty * house.ElectricityUnitPrice;
        var waterAmount = waterQty * house.WaterUnitPrice;

        var depositRefunded = Math.Max(0, lease.DepositHeld - request.DepositDeducted - eleAmount - waterAmount);
        var now = time.GetUtcNow();

        await using var scope = await database.BeginTransactionAsync(cancellationToken);

        lease.Status = LeaseStatus.Terminated;
        lease.EndedAt = now;
        lease.EndReason = request.EndReason?.Trim();
        lease.FinalElectricityReading = request.FinalElectricityReading;
        lease.FinalWaterReading = request.FinalWaterReading;
        lease.DepositDeducted = request.DepositDeducted;
        lease.DepositRefunded = depositRefunded;

        foreach (var t in lease.Tenants.Where(t => t.MovedOutAt == null))
        {
            t.MovedOutAt = now;
        }

        room.CurrentElectricityReading = request.FinalElectricityReading;
        room.CurrentWaterReading = request.FinalWaterReading;

        // Room status rule §9.3: if no other active deposit or lease, room becomes Available.
        var hasOtherActiveLease = await database.Leases.AnyAsync(
            l => l.RoomId == room.Id && l.Id != lease.Id && l.Status == LeaseStatus.Active, cancellationToken);

        var hasActiveDeposit = await database.Deposits.AnyAsync(
            d => d.RoomId == room.Id &&
                 (d.Status == DepositStatus.Accepted || d.Status == DepositStatus.Paid), cancellationToken);

        if (!hasOtherActiveLease && !hasActiveDeposit && room.Status != RoomStatus.Maintenance)
        {
            room.Status = RoomStatus.Available;
        }

        await database.SaveChangesAsync(cancellationToken);
        await scope.CommitAsync(cancellationToken);

        return await LeaseRules.LoadAsync(database, lease.Id, cancellationToken);
    }
}