using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Extensions;

internal static class ExtensionRules
{
    internal static IQueryable<ExtensionRequestResponse> Project(
        IAppDbContext database,
        IQueryable<ExtensionRequest> query) =>
        query.Select(e => new ExtensionRequestResponse(
            e.Id,
            e.LeaseId,
            e.Lease.RoomId,
            e.Lease.Room.RoomNumber,
            e.Lease.Room.BoardingHouseId,
            e.Lease.Room.BoardingHouse.Name,
            e.RequestedByUserId,
            database.Users.Where(u => u.Id == e.RequestedByUserId).Select(u => u.FullName).FirstOrDefault() ?? "",
            e.CurrentEndDate,
            e.RequestedEndDate,
            e.Status,
            e.TenantNote,
            e.OwnerNote,
            e.HandledByUserId,
            e.CreatedAt));

    internal static async Task<ExtensionRequestResponse> LoadAsync(
        IAppDbContext database,
        Guid extensionId,
        CancellationToken cancellationToken) =>
        await Project(database, database.ExtensionRequests.AsNoTracking().Where(e => e.Id == extensionId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Extension.NotFound);
}

public sealed class ListExtensionRequestsHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<ExtensionRequestResponse>> HandleAsync(
        RequestStatus? status = null,
        Guid? boardingHouseId = null,
        int page = 1,
        int pageSize = Paged.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var query = database.ExtensionRequests.AsNoTracking().AsQueryable();

        if (currentUser.Role == UserRole.Tenant)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(e => e.RequestedByUserId == userId);
        }
        else if (boardingHouseId.HasValue)
        {
            await access.RequireStaffOrOwnerAsync(boardingHouseId.Value, cancellationToken);
            query = query.Where(e => e.Lease.Room.BoardingHouseId == boardingHouseId.Value);
        }
        else
        {
            var managedHouseIds = access.Managed().Select(b => b.Id);
            query = query.Where(e => managedHouseIds.Contains(e.Lease.Room.BoardingHouseId));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        return await Paged.FromAsync(
            ExtensionRules.Project(database, query.OrderByDescending(e => e.CreatedAt)),
            page,
            pageSize,
            cancellationToken);
    }
}

public sealed class GetExtensionRequestHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<ExtensionRequestResponse> HandleAsync(
        Guid extensionId,
        CancellationToken cancellationToken = default)
    {
        var ext = await database.ExtensionRequests
            .AsNoTracking()
            .Where(e => e.Id == extensionId)
            .Select(e => new
            {
                e.RequestedByUserId,
                e.Lease.Room.BoardingHouseId
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Extension.NotFound);

        if (currentUser.Role == UserRole.Tenant)
        {
            if (ext.RequestedByUserId != currentUser.RequireUserId())
            {
                throw new ForbiddenException(MessageKeys.Extension.NotYours);
            }
        }
        else
        {
            await access.RequireStaffOrOwnerAsync(ext.BoardingHouseId, cancellationToken);
        }

        return await ExtensionRules.LoadAsync(database, extensionId, cancellationToken);
    }
}

public sealed class CreateExtensionRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<ExtensionRequestResponse> HandleAsync(
        CreateExtensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var lease = await database.Leases
            .Include(l => l.Room)
            .ThenInclude(r => r.BoardingHouse)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Lease.NotFound);

        if (lease.PrimaryTenantUserId != userId)
        {
            throw new ForbiddenException(MessageKeys.Lease.NotYours);
        }

        if (lease.Status != LeaseStatus.Active && lease.Status != LeaseStatus.Expiring)
        {
            throw new BusinessRuleException(MessageKeys.Lease.NotActive);
        }

        if (request.RequestedEndDate <= lease.EndDate)
        {
            throw new BusinessRuleException(MessageKeys.Extension.EndDateMustBeAfterCurrent);
        }

        var hasPending = await database.ExtensionRequests.AnyAsync(
            e => e.LeaseId == lease.Id && e.Status == RequestStatus.Pending, cancellationToken);

        if (hasPending)
        {
            throw new ConflictException(MessageKeys.Extension.AlreadyPending);
        }

        var extension = new ExtensionRequest
        {
            LeaseId = lease.Id,
            RequestedByUserId = userId,
            CurrentEndDate = lease.EndDate,
            RequestedEndDate = request.RequestedEndDate,
            Status = RequestStatus.Pending,
            TenantNote = request.TenantNote?.Trim()
        };

        database.ExtensionRequests.Add(extension);
        await database.SaveChangesAsync(cancellationToken);

        return await ExtensionRules.LoadAsync(database, extension.Id, cancellationToken);
    }
}

public sealed class ApproveExtensionRequestHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    NotificationDispatcher notifications,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<ExtensionRequestResponse> HandleAsync(
        Guid extensionId,
        CancellationToken cancellationToken = default)
    {
        var extension = await database.ExtensionRequests
            .Include(e => e.Lease)
            .ThenInclude(l => l.Room)
            .ThenInclude(r => r.BoardingHouse)
            .FirstOrDefaultAsync(e => e.Id == extensionId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Extension.NotFound);

        var house = extension.Lease.Room.BoardingHouse;
        await access.RequireStaffOrOwnerAsync(house.Id, cancellationToken);

        if (extension.Status != RequestStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Extension.NotPending);
        }

        extension.Status = RequestStatus.Accepted;
        extension.HandledByUserId = currentUser.RequireUserId();

        var lease = extension.Lease;
        lease.EndDate = extension.RequestedEndDate;

        var today = DateOnly.FromDateTime(time.GetUtcNow().DateTime);
        var daysLeft = (lease.EndDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).TotalDays;
        if (daysLeft > 30 && lease.Status == LeaseStatus.Expiring)
        {
            lease.Status = LeaseStatus.Active;
        }

        notifications.Queue(
            lease.PrimaryTenantUserId,
            NotificationType.ExtensionHandled,
            new
            {
                roomNumber = lease.Room.RoomNumber,
                boardingHouseName = house.Name
            },
            linkUrl: $"/leases/{lease.Id}");

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        return await ExtensionRules.LoadAsync(database, extension.Id, cancellationToken);
    }
}

public sealed class RejectExtensionRequestHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    NotificationDispatcher notifications,
    ICurrentUser currentUser)
{
    public async Task<ExtensionRequestResponse> HandleAsync(
        Guid extensionId,
        RejectExtensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var extension = await database.ExtensionRequests
            .Include(e => e.Lease)
            .ThenInclude(l => l.Room)
            .ThenInclude(r => r.BoardingHouse)
            .FirstOrDefaultAsync(e => e.Id == extensionId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Extension.NotFound);

        var house = extension.Lease.Room.BoardingHouse;
        await access.RequireStaffOrOwnerAsync(house.Id, cancellationToken);

        if (extension.Status != RequestStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Extension.NotPending);
        }

        extension.Status = RequestStatus.Rejected;
        extension.OwnerNote = request.OwnerNote?.Trim();
        extension.HandledByUserId = currentUser.RequireUserId();

        var lease = extension.Lease;

        notifications.Queue(
            lease.PrimaryTenantUserId,
            NotificationType.ExtensionHandled,
            new
            {
                roomNumber = lease.Room.RoomNumber,
                boardingHouseName = house.Name
            },
            linkUrl: $"/leases/{lease.Id}");

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        return await ExtensionRules.LoadAsync(database, extension.Id, cancellationToken);
    }
}

