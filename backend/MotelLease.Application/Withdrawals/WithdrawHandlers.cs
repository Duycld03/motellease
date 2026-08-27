using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Notifications;
using MotelLease.Application.Withdrawals.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Withdrawals;

public sealed class ListWithdrawRequestsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<WithdrawRequestResponse>> HandleAsync(
        RequestStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var query = database.WithdrawRequests
            .AsNoTracking()
            .Include(w => w.OwnerUser)
            .AsQueryable();

        if (role == UserRole.Owner)
        {
            query = query.Where(w => w.OwnerUserId == userId);
        }

        if (status.HasValue)
        {
            query = query.Where(w => w.Status == status.Value);
        }

        var projected = query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WithdrawRequestResponse(
                w.Id,
                w.OwnerUserId,
                w.OwnerUser.FullName,
                w.Amount,
                w.BankName,
                w.BankAccountNumber,
                w.BankAccountHolder,
                w.Status,
                w.ProcessedByUserId,
                database.Users.Where(u => u.Id == w.ProcessedByUserId).Select(u => u.FullName).FirstOrDefault(),
                w.ProcessedAt,
                w.RejectReason,
                w.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class CreateWithdrawRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<WithdrawRequestResponse> HandleAsync(
        CreateWithdrawRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var profile = await database.OwnerProfiles
            .Include(op => op.User)
            .FirstOrDefaultAsync(op => op.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        // Invariant §9.11: A withdraw request may never exceed OwnerProfile.AvailableBalance
        if (request.Amount > profile.AvailableBalance)
        {
            throw new BusinessRuleException(MessageKeys.Withdraw.InsufficientBalance);
        }

        var bankName = (request.BankName ?? profile.BankName)?.Trim();
        var bankAccountNumber = (request.BankAccountNumber ?? profile.BankAccountNumber)?.Trim();
        var bankAccountHolder = (request.BankAccountHolder ?? profile.BankAccountHolder)?.Trim();

        if (string.IsNullOrWhiteSpace(bankName) ||
            string.IsNullOrWhiteSpace(bankAccountNumber) ||
            string.IsNullOrWhiteSpace(bankAccountHolder))
        {
            throw new BusinessRuleException(MessageKeys.Withdraw.BankDetailsRequired);
        }

        // Hold balance immediately during pending request
        profile.AvailableBalance -= request.Amount;

        var withdraw = new WithdrawRequest
        {
            OwnerUserId = userId,
            Amount = request.Amount,
            BankName = bankName,
            BankAccountNumber = bankAccountNumber,
            BankAccountHolder = bankAccountHolder,
            Status = RequestStatus.Pending
        };

        database.WithdrawRequests.Add(withdraw);
        await database.SaveChangesAsync(cancellationToken);

        return new WithdrawRequestResponse(
            withdraw.Id,
            withdraw.OwnerUserId,
            profile.User.FullName,
            withdraw.Amount,
            withdraw.BankName,
            withdraw.BankAccountNumber,
            withdraw.BankAccountHolder,
            withdraw.Status,
            null,
            null,
            null,
            null,
            withdraw.CreatedAt);
    }
}

public sealed class GetWithdrawRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<WithdrawRequestResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var withdraw = await database.WithdrawRequests
            .AsNoTracking()
            .Include(w => w.OwnerUser)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Withdraw.NotFound);

        var hasAccess = role switch
        {
            UserRole.Admin => true,
            UserRole.Owner => withdraw.OwnerUserId == userId,
            _ => false
        };

        if (!hasAccess)
        {
            throw new ForbiddenException(MessageKeys.Withdraw.NotYours);
        }

        string? processedByName = null;
        if (withdraw.ProcessedByUserId.HasValue)
        {
            processedByName = await database.Users
                .AsNoTracking()
                .Where(u => u.Id == withdraw.ProcessedByUserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new WithdrawRequestResponse(
            withdraw.Id,
            withdraw.OwnerUserId,
            withdraw.OwnerUser.FullName,
            withdraw.Amount,
            withdraw.BankName,
            withdraw.BankAccountNumber,
            withdraw.BankAccountHolder,
            withdraw.Status,
            withdraw.ProcessedByUserId,
            processedByName,
            withdraw.ProcessedAt,
            withdraw.RejectReason,
            withdraw.CreatedAt);
    }
}

public sealed class ApproveWithdrawRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time,
    NotificationDispatcher notifications)
{
    public async Task<WithdrawRequestResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var withdraw = await database.WithdrawRequests
            .Include(w => w.OwnerUser)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Withdraw.NotFound);

        if (withdraw.Status != RequestStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Withdraw.AlreadyProcessed);
        }

        withdraw.Status = RequestStatus.Accepted;
        withdraw.ProcessedByUserId = adminId;
        withdraw.ProcessedAt = time.GetUtcNow();

        notifications.Queue(
            withdraw.OwnerUserId,
            NotificationType.WithdrawHandled,
            new
            {
                amount = withdraw.Amount,
                status = "accepted"
            },
            $"/withdraw-requests/{withdraw.Id}");

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        var admin = await database.Users.AsNoTracking().FirstAsync(u => u.Id == adminId, cancellationToken);

        return new WithdrawRequestResponse(
            withdraw.Id,
            withdraw.OwnerUserId,
            withdraw.OwnerUser.FullName,
            withdraw.Amount,
            withdraw.BankName,
            withdraw.BankAccountNumber,
            withdraw.BankAccountHolder,
            withdraw.Status,
            withdraw.ProcessedByUserId,
            admin.FullName,
            withdraw.ProcessedAt,
            withdraw.RejectReason,
            withdraw.CreatedAt);
    }
}

public sealed class RejectWithdrawRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time,
    NotificationDispatcher notifications)
{
    public async Task<WithdrawRequestResponse> HandleAsync(
        Guid id,
        RejectWithdrawRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var withdraw = await database.WithdrawRequests
            .Include(w => w.OwnerUser)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Withdraw.NotFound);

        if (withdraw.Status != RequestStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Withdraw.AlreadyProcessed);
        }

        var profile = await database.OwnerProfiles
            .FirstOrDefaultAsync(op => op.UserId == withdraw.OwnerUserId, cancellationToken);

        if (profile != null)
        {
            profile.AvailableBalance += withdraw.Amount;
        }

        withdraw.Status = RequestStatus.Rejected;
        withdraw.RejectReason = request.Reason?.Trim();
        withdraw.ProcessedByUserId = adminId;
        withdraw.ProcessedAt = time.GetUtcNow();

        notifications.Queue(
            withdraw.OwnerUserId,
            NotificationType.WithdrawHandled,
            new
            {
                amount = withdraw.Amount,
                status = "rejected"
            },
            $"/withdraw-requests/{withdraw.Id}");

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        var admin = await database.Users.AsNoTracking().FirstAsync(u => u.Id == adminId, cancellationToken);

        return new WithdrawRequestResponse(
            withdraw.Id,
            withdraw.OwnerUserId,
            withdraw.OwnerUser.FullName,
            withdraw.Amount,
            withdraw.BankName,
            withdraw.BankAccountNumber,
            withdraw.BankAccountHolder,
            withdraw.Status,
            withdraw.ProcessedByUserId,
            admin.FullName,
            withdraw.ProcessedAt,
            withdraw.RejectReason,
            withdraw.CreatedAt);
    }
}
