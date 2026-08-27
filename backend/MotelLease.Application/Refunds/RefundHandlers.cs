using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Notifications;
using MotelLease.Application.Refunds.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Refunds;

public sealed class ListRefundRequestsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<RefundRequestResponse>> HandleAsync(
        RequestStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var query = database.RefundRequests
            .AsNoTracking()
            .Include(r => r.Deposit)
                .ThenInclude(d => d.Room)
                    .ThenInclude(rm => rm.BoardingHouse)
            .Include(r => r.User)
            .AsQueryable();

        if (role == UserRole.Tenant)
        {
            query = query.Where(r => r.UserId == userId);
        }
        else if (role == UserRole.Owner)
        {
            query = query.Where(r => r.Deposit.Room.BoardingHouse.OwnerUserId == userId);
        }
        else if (role == UserRole.Staff)
        {
            var assignedHouseIds = database.StaffAssignments
                .Where(sa => sa.StaffUserId == userId && sa.UnassignedAt == null)
                .Select(sa => sa.BoardingHouseId);

            query = query.Where(r => assignedHouseIds.Contains(r.Deposit.Room.BoardingHouseId));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var projected = query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RefundRequestResponse(
                r.Id,
                r.DepositId,
                r.LeaseId,
                r.Deposit.RoomId,
                r.Deposit.Room.RoomNumber,
                r.Deposit.Room.BoardingHouseId,
                r.Deposit.Room.BoardingHouse.Name,
                r.UserId,
                r.User.FullName,
                r.Amount,
                r.Status,
                r.Reason,
                r.ProcessedByUserId,
                database.Users.Where(u => u.Id == r.ProcessedByUserId).Select(u => u.FullName).FirstOrDefault(),
                r.ProcessedAt,
                r.RejectReason,
                r.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class CreateRefundRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<RefundRequestResponse> HandleAsync(
        CreateRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var deposit = await database.Deposits
            .Include(d => d.Room)
                .ThenInclude(r => r.BoardingHouse)
            .FirstOrDefaultAsync(d => d.Id == request.DepositId && d.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Deposit.NotFound);

        if (deposit.Status != DepositStatus.Paid)
        {
            throw new BusinessRuleException(MessageKeys.Refund.InvalidDepositState);
        }

        var alreadyPending = await database.RefundRequests
            .AnyAsync(r => r.DepositId == deposit.Id && r.Status == RequestStatus.Pending, cancellationToken);

        if (alreadyPending)
        {
            throw new ConflictException(MessageKeys.Refund.AlreadyRequested);
        }

        var refund = new RefundRequest
        {
            DepositId = deposit.Id,
            UserId = userId,
            Amount = deposit.Amount,
            Status = RequestStatus.Pending,
            Reason = request.Reason?.Trim()
        };

        deposit.Status = DepositStatus.Refunding;
        database.RefundRequests.Add(refund);

        await database.SaveChangesAsync(cancellationToken);

        var user = await database.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new RefundRequestResponse(
            refund.Id,
            refund.DepositId,
            refund.LeaseId,
            deposit.RoomId,
            deposit.Room.RoomNumber,
            deposit.Room.BoardingHouseId,
            deposit.Room.BoardingHouse.Name,
            user.Id,
            user.FullName,
            refund.Amount,
            refund.Status,
            refund.Reason,
            null,
            null,
            null,
            null,
            refund.CreatedAt);
    }
}

public sealed class GetRefundRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<RefundRequestResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var refund = await database.RefundRequests
            .AsNoTracking()
            .Include(r => r.Deposit)
                .ThenInclude(d => d.Room)
                    .ThenInclude(rm => rm.BoardingHouse)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Refund.NotFound);

        var isAssignedStaff = role == UserRole.Staff && await database.StaffAssignments
            .AnyAsync(sa => sa.BoardingHouseId == refund.Deposit.Room.BoardingHouseId && sa.StaffUserId == userId && sa.UnassignedAt == null, cancellationToken);

        var hasAccess = role switch
        {
            UserRole.Admin => true,
            UserRole.Tenant => refund.UserId == userId,
            UserRole.Owner => refund.Deposit.Room.BoardingHouse.OwnerUserId == userId,
            UserRole.Staff => isAssignedStaff,
            _ => false
        };

        if (!hasAccess)
        {
            throw new ForbiddenException(MessageKeys.Refund.NotYours);
        }

        string? processedByName = null;
        if (refund.ProcessedByUserId.HasValue)
        {
            processedByName = await database.Users
                .AsNoTracking()
                .Where(u => u.Id == refund.ProcessedByUserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new RefundRequestResponse(
            refund.Id,
            refund.DepositId,
            refund.LeaseId,
            refund.Deposit.RoomId,
            refund.Deposit.Room.RoomNumber,
            refund.Deposit.Room.BoardingHouseId,
            refund.Deposit.Room.BoardingHouse.Name,
            refund.UserId,
            refund.User.FullName,
            refund.Amount,
            refund.Status,
            refund.Reason,
            refund.ProcessedByUserId,
            processedByName,
            refund.ProcessedAt,
            refund.RejectReason,
            refund.CreatedAt);
    }
}

public sealed class ApproveRefundRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time,
    NotificationDispatcher notifications)
{
    public async Task<RefundRequestResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var refund = await database.RefundRequests
            .Include(r => r.Deposit)
                .ThenInclude(d => d.Room)
                    .ThenInclude(rm => rm.BoardingHouse)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Refund.NotFound);

        if (role != UserRole.Admin && refund.Deposit.Room.BoardingHouse.OwnerUserId != userId)
        {
            throw new ForbiddenException(MessageKeys.Refund.NotYours);
        }

        if (refund.Status != RequestStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Refund.AlreadyProcessed);
        }

        refund.Status = RequestStatus.Accepted;
        refund.ProcessedByUserId = userId;
        refund.ProcessedAt = time.GetUtcNow();
        refund.Deposit.Status = DepositStatus.Refunded;

        notifications.Queue(
            refund.UserId,
            NotificationType.RefundProcessed,
            new
            {
                amount = refund.Amount,
                status = "accepted"
            },
            $"/deposits/{refund.DepositId}");

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        var approver = await database.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new RefundRequestResponse(
            refund.Id,
            refund.DepositId,
            refund.LeaseId,
            refund.Deposit.RoomId,
            refund.Deposit.Room.RoomNumber,
            refund.Deposit.Room.BoardingHouseId,
            refund.Deposit.Room.BoardingHouse.Name,
            refund.UserId,
            refund.User.FullName,
            refund.Amount,
            refund.Status,
            refund.Reason,
            refund.ProcessedByUserId,
            approver.FullName,
            refund.ProcessedAt,
            refund.RejectReason,
            refund.CreatedAt);
    }
}

public sealed class RejectRefundRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time,
    NotificationDispatcher notifications)
{
    public async Task<RefundRequestResponse> HandleAsync(
        Guid id,
        RejectRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var refund = await database.RefundRequests
            .Include(r => r.Deposit)
                .ThenInclude(d => d.Room)
                    .ThenInclude(rm => rm.BoardingHouse)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Refund.NotFound);

        if (role != UserRole.Admin && refund.Deposit.Room.BoardingHouse.OwnerUserId != userId)
        {
            throw new ForbiddenException(MessageKeys.Refund.NotYours);
        }

        if (refund.Status != RequestStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Refund.AlreadyProcessed);
        }

        refund.Status = RequestStatus.Rejected;
        refund.RejectReason = request.Reason?.Trim();
        refund.ProcessedByUserId = userId;
        refund.ProcessedAt = time.GetUtcNow();
        refund.Deposit.Status = DepositStatus.Paid;

        notifications.Queue(
            refund.UserId,
            NotificationType.RefundProcessed,
            new
            {
                amount = refund.Amount,
                status = "rejected"
            },
            $"/deposits/{refund.DepositId}");

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        var rejecter = await database.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new RefundRequestResponse(
            refund.Id,
            refund.DepositId,
            refund.LeaseId,
            refund.Deposit.RoomId,
            refund.Deposit.Room.RoomNumber,
            refund.Deposit.Room.BoardingHouseId,
            refund.Deposit.Room.BoardingHouse.Name,
            refund.UserId,
            refund.User.FullName,
            refund.Amount,
            refund.Status,
            refund.Reason,
            refund.ProcessedByUserId,
            rejecter.FullName,
            refund.ProcessedAt,
            refund.RejectReason,
            refund.CreatedAt);
    }
}
