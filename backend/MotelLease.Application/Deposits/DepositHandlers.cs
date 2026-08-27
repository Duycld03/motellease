using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Deposits.Contracts;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Domain.Rooms;

namespace MotelLease.Application.Deposits;

/// <summary>
/// How long an accepted request waits for its payment before it stops holding the room
/// (docs/domain-rules.md §2). Long enough to arrange the money, short enough that a room is not
/// taken off the market for a request nobody intends to pay.
/// </summary>
public sealed record DepositPaymentWindow(TimeSpan Lifetime)
{
    public static readonly DepositPaymentWindow Default = new(TimeSpan.FromHours(24));
}

internal static class DepositRules
{
    /// <summary>A request that is holding, or still able to hold, the room it points at.</summary>
    internal static bool IsLive(DepositStatus status) =>
        status is DepositStatus.Pending or DepositStatus.Accepted or DepositStatus.Paid;

    internal static IQueryable<DepositResponse> Project(IQueryable<Deposit> query) =>
        query.Select(d => new DepositResponse(
            d.Id,
            d.RoomId,
            d.Room.RoomNumber,
            d.Room.BoardingHouseId,
            d.Room.BoardingHouse.Name,
            d.UserId,
            d.User.FullName,
            d.User.PhoneNumber,
            d.Amount,
            d.Status,
            d.RequestedStartDate,
            d.RequestedTermMonths,
            d.ExpiresAt,
            d.ReasonForCancel,
            d.HandledByUserId,
            d.CreatedAt));

    internal static async Task<DepositResponse> LoadAsync(
        IAppDbContext database,
        Guid depositId,
        CancellationToken cancellationToken) =>
        await Project(database.Deposits.AsNoTracking().Where(d => d.Id == depositId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Deposit.NotFound);

    internal static async Task<Deposit> RequireAsync(
        IAppDbContext database,
        Guid depositId,
        CancellationToken cancellationToken) =>
        await database.Deposits.FirstOrDefaultAsync(d => d.Id == depositId, cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Deposit.NotFound);

    /// <summary>
    /// The property a request belongs to, read without the soft-delete filter: a room may have been
    /// taken off the listing after the request was made, and the request still has to resolve to the
    /// property whose owner answers for it.
    /// </summary>
    internal static async Task<Guid> BoardingHouseIdAsync(
        IAppDbContext database,
        Guid roomId,
        CancellationToken cancellationToken) =>
        await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == roomId)
            .Select(r => r.BoardingHouseId)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Re-derives the room's status from the rows that commit it (docs/domain-rules.md §9.3). Called
    /// after any change to a deposit's hold, so the column stays a summary of the rows rather than a
    /// second source of truth free to drift from them.
    ///
    /// The changed row is read from memory and excluded from the query: its new status has not been
    /// saved yet, so asking the database about it would answer with the state being replaced.
    /// </summary>
    internal static async Task SyncRoomStatusAsync(
        IAppDbContext database,
        Deposit changed,
        CancellationToken cancellationToken)
    {
        var room = await database.Rooms.FirstOrDefaultAsync(
            r => r.Id == changed.RoomId, cancellationToken);

        if (room is null)
        {
            return;
        }

        var hasLiveLease = await database.Leases.AnyAsync(
            l => l.RoomId == changed.RoomId
                 && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring),
            cancellationToken);

        var hasHoldingDeposit = IsHolding(changed.Status)
            || await database.Deposits.AnyAsync(
                d => d.RoomId == changed.RoomId
                     && d.Id != changed.Id
                     && (d.Status == DepositStatus.Accepted || d.Status == DepositStatus.Paid),
                cancellationToken);

        room.Status = RoomStatusPolicy.DeriveFromCommitments(hasLiveLease, hasHoldingDeposit);
    }

    private static bool IsHolding(DepositStatus status) =>
        status is DepositStatus.Accepted or DepositStatus.Paid;

    /// <summary>
    /// What the tenant is told when their request is answered. The payload is stored, not rendered,
    /// so the sentence is built in the reader's language when they open it (§7).
    /// </summary>
    internal static async Task NotifyTenantAsync(
        IAppDbContext database,
        NotificationDispatcher notifications,
        Deposit deposit,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var room = await RoomLabelAsync(database, deposit.RoomId, cancellationToken);

        notifications.Queue(
            deposit.UserId,
            type,
            new
            {
                depositId = deposit.Id,
                status = deposit.Status.ToString(),
                roomNumber = room.RoomNumber,
                boardingHouseName = room.HouseName,
                amount = deposit.Amount,
                expiresAt = deposit.ExpiresAt
            },
            linkUrl: $"/deposits/{deposit.Id}");
    }

    /// <summary>
    /// A new request goes to the owner and to whoever is currently assigned to the property: either
    /// of them can answer it, so leaving one of them out would let a request sit unanswered.
    /// </summary>
    internal static async Task NotifyPropertyAsync(
        IAppDbContext database,
        NotificationDispatcher notifications,
        Deposit deposit,
        Guid boardingHouseId,
        CancellationToken cancellationToken)
    {
        var room = await RoomLabelAsync(database, deposit.RoomId, cancellationToken);

        var recipients = await database.BoardingHouses
            .IgnoreQueryFilters()
            .Where(b => b.Id == boardingHouseId)
            .Select(b => b.OwnerUserId)
            .Concat(database.StaffAssignments
                .Where(a => a.BoardingHouseId == boardingHouseId && a.UnassignedAt == null)
                .Select(a => a.StaffUserId))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            notifications.Queue(
                recipient,
                NotificationType.DepositRequested,
                new
                {
                    depositId = deposit.Id,
                    roomNumber = room.RoomNumber,
                    boardingHouseName = room.HouseName,
                    amount = deposit.Amount,
                    requestedStartDate = deposit.RequestedStartDate,
                    requestedTermMonths = deposit.RequestedTermMonths
                },
                linkUrl: $"/deposits/{deposit.Id}");
        }
    }

    private static async Task<RoomLabel> RoomLabelAsync(
        IAppDbContext database,
        Guid roomId,
        CancellationToken cancellationToken) =>
        await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == roomId)
            .Select(r => new RoomLabel(r.RoomNumber, r.BoardingHouse.Name))
            .FirstAsync(cancellationToken);

    private sealed record RoomLabel(string RoomNumber, string HouseName);
}

/// <summary>
/// GET /deposits. A tenant sees the requests they made, an owner or staff member the ones on the
/// properties they run. Same endpoint, different rows — the role decides the scope, not the path
/// (docs/api-design.md).
/// </summary>
public sealed class ListDepositsHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<DepositResponse>> HandleAsync(
        DepositStatus? status,
        Guid? boardingHouseId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var query = currentUser.Role == UserRole.Tenant
            ? database.Deposits.Where(d => d.UserId == userId)
            : database.Deposits.Where(d =>
                access.Managed().Any(b => b.Id == d.Room.BoardingHouseId));

        if (status is { } wanted)
        {
            query = query.Where(d => d.Status == wanted);
        }

        if (boardingHouseId is { } house)
        {
            query = query.Where(d => d.Room.BoardingHouseId == house);
        }

        return await Paged.FromAsync(
            DepositRules.Project(query.AsNoTracking().OrderByDescending(d => d.CreatedAt)),
            page,
            pageSize,
            cancellationToken);
    }
}

/// <summary>
/// GET /deposits/{id}. Reachable by the tenant who made it and by whoever runs the property;
/// anybody else is refused by the boarding house check.
/// </summary>
public sealed class GetDepositHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<DepositResponse> HandleAsync(
        Guid depositId,
        CancellationToken cancellationToken = default)
    {
        var deposit = await DepositRules.RequireAsync(database, depositId, cancellationToken);

        if (deposit.UserId != currentUser.RequireUserId())
        {
            await access.RequireStaffOrOwnerAsync(
                await DepositRules.BoardingHouseIdAsync(
                    database, deposit.RoomId, cancellationToken),
                cancellationToken);
        }

        return await DepositRules.LoadAsync(database, deposit.Id, cancellationToken);
    }
}

/// <summary>
/// POST /deposits. Only a vacant room of a published listing can be asked for, and the amount owed
/// is frozen here: one month of the room type's price, copied onto the row. Reading the price again
/// at payment or contract time would let an already-agreed figure change after the fact
/// (docs/domain-rules.md §2).
/// </summary>
public sealed class RequestDepositHandler(
    IAppDbContext database,
    NotificationDispatcher notifications,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<DepositResponse> HandleAsync(
        RequestDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        if (request.RequestedStartDate < today)
        {
            throw new BusinessRuleException(MessageKeys.Deposit.StartDateInPast);
        }

        var room = await database.Rooms
            .Where(r => r.Id == request.RoomId)
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.BoardingHouseId,
                r.RoomType.Price,
                r.BoardingHouse.ListingStatus
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        if (room.ListingStatus != ListingStatus.Published)
        {
            throw new BusinessRuleException(MessageKeys.Deposit.ListingNotPublished);
        }

        if (room.Status != RoomStatus.Available)
        {
            throw new BusinessRuleException(MessageKeys.Deposit.RoomNotAvailable);
        }

        // The same person cannot hold two live requests on the same room (§2): the second one
        // holds nothing extra and only gives the owner two things to answer.
        var live = await database.Deposits
            .Where(d => d.RoomId == room.Id && d.UserId == userId)
            .Select(d => d.Status)
            .ToListAsync(cancellationToken);

        if (live.Any(DepositRules.IsLive))
        {
            throw new ConflictException(MessageKeys.Deposit.AlreadyRequested);
        }

        var deposit = new Deposit
        {
            UserId = userId,
            RoomId = room.Id,
            Amount = room.Price,
            Status = DepositStatus.Pending,
            RequestedStartDate = request.RequestedStartDate,
            RequestedTermMonths = request.RequestedTermMonths
        };

        database.Deposits.Add(deposit);

        await DepositRules.NotifyPropertyAsync(
            database, notifications, deposit, room.BoardingHouseId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);

        await notifications.DeliverAsync(cancellationToken);

        return await DepositRules.LoadAsync(database, deposit.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /deposits/{id}/approve and /reject. Accepting puts the tenant on the clock: ExpiresAt is set
/// from <see cref="DepositPaymentWindow"/> and the room moves to Reserved, so nobody else can be
/// promised it while the payment is outstanding. Rejecting leaves the room as it was.
/// </summary>
public sealed class AnswerDepositHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    NotificationDispatcher notifications,
    ICurrentUser currentUser,
    DepositPaymentWindow window,
    TimeProvider time)
{
    public Task<DepositResponse> ApproveAsync(
        Guid depositId,
        CancellationToken cancellationToken = default) =>
        AnswerAsync(depositId, DepositStatus.Accepted, reason: null, cancellationToken);

    public Task<DepositResponse> RejectAsync(
        Guid depositId,
        RejectDepositRequest request,
        CancellationToken cancellationToken = default) =>
        AnswerAsync(depositId, DepositStatus.Rejected, request.Reason.Trim(), cancellationToken);

    private async Task<DepositResponse> AnswerAsync(
        Guid depositId,
        DepositStatus answer,
        string? reason,
        CancellationToken cancellationToken)
    {
        var deposit = await DepositRules.RequireAsync(database, depositId, cancellationToken);

        await access.RequireStaffOrOwnerAsync(
            await DepositRules.BoardingHouseIdAsync(database, deposit.RoomId, cancellationToken),
            cancellationToken);

        if (deposit.Status != DepositStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Deposit.NotPending);
        }

        // A room can only be promised once. Another request on the same room may have been accepted
        // while this one waited, and the owner has to be told that rather than silently overriding
        // the hold that already exists.
        if (answer == DepositStatus.Accepted
            && await RoomIsTakenAsync(deposit, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Deposit.RoomNotAvailable);
        }

        deposit.Status = answer;
        deposit.HandledByUserId = currentUser.RequireUserId();

        if (answer == DepositStatus.Accepted)
        {
            deposit.ExpiresAt = time.GetUtcNow() + window.Lifetime;
        }

        // The only free-text field on the row. A rejection reason and a cancellation reason are the
        // same thing from the tenant's side: why the room is not being held for them.
        if (reason is not null)
        {
            deposit.ReasonForCancel = reason;
        }

        await DepositRules.SyncRoomStatusAsync(database, deposit, cancellationToken);

        await DepositRules.NotifyTenantAsync(
            database,
            notifications,
            deposit,
            answer == DepositStatus.Accepted
                ? NotificationType.DepositAccepted
                : NotificationType.DepositRejected,
            cancellationToken);

        await database.SaveChangesAsync(cancellationToken);

        await notifications.DeliverAsync(cancellationToken);

        return await DepositRules.LoadAsync(database, deposit.Id, cancellationToken);
    }

    /// <summary>
    /// Whether the room can still be promised. Read from the room's own status, which §9.3 keeps as
    /// the summary of the lease and deposit rows: between the request and this answer the room may
    /// have been leased, held for somebody else, or taken off the listing altogether.
    /// </summary>
    private async Task<bool> RoomIsTakenAsync(
        Deposit deposit,
        CancellationToken cancellationToken)
    {
        var room = await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == deposit.RoomId)
            .Select(r => new { r.Status, r.IsDeleted })
            .FirstOrDefaultAsync(cancellationToken);

        return room is null || room.IsDeleted || room.Status != RoomStatus.Available;
    }
}

/// <summary>
/// PUT /deposits/{id}/cancel. The tenant's own withdrawal, allowed while nothing has been paid.
/// Once the money is in, walking away is a refund rather than a cancellation and goes through the
/// refund flow instead.
///
/// The withdrawal is recorded as Rejected with the reason on the row: DepositStatus has no Cancelled
/// value (docs/erd.md §3), and from the room's side the outcome is identical — the hold is gone.
/// </summary>
public sealed class CancelDepositHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<DepositResponse> HandleAsync(
        Guid depositId,
        CancelDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        var deposit = await DepositRules.RequireAsync(database, depositId, cancellationToken);

        if (deposit.UserId != currentUser.RequireUserId())
        {
            throw new ForbiddenException(MessageKeys.Deposit.NotYours);
        }

        if (deposit.Status is not (DepositStatus.Pending or DepositStatus.Accepted))
        {
            throw new BusinessRuleException(MessageKeys.Deposit.NotCancellable);
        }

        deposit.Status = DepositStatus.Rejected;
        deposit.ReasonForCancel = request.Reason?.Trim();
        deposit.ExpiresAt = null;

        await DepositRules.SyncRoomStatusAsync(database, deposit, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);

        return await DepositRules.LoadAsync(database, deposit.Id, cancellationToken);
    }
}

/// <summary>
/// GET /deposits/{id}/contract-preview. What the tenant would be signing, shown before they pay.
/// Every figure is read from the deposit row rather than from the room type, so the preview shows
/// the amount that was agreed and not whatever the listing asks for today.
/// </summary>
public sealed class PreviewDepositContractHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<DepositContractPreviewResponse> HandleAsync(
        Guid depositId,
        CancellationToken cancellationToken = default)
    {
        var deposit = await DepositRules.RequireAsync(database, depositId, cancellationToken);

        if (deposit.UserId != currentUser.RequireUserId())
        {
            throw new ForbiddenException(MessageKeys.Deposit.NotYours);
        }

        // Before the owner accepts there are no agreed terms to preview, only a request.
        if (deposit.Status is DepositStatus.Pending
            or DepositStatus.Rejected
            or DepositStatus.Expired)
        {
            throw new BusinessRuleException(MessageKeys.Deposit.NotAccepted);
        }

        var room = await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == deposit.RoomId)
            .Select(r => new
            {
                r.RoomNumber,
                House = r.BoardingHouse
            })
            .FirstAsync(cancellationToken);

        var tenant = await database.Users
            .Where(u => u.Id == deposit.UserId)
            .Select(u => new { u.FullName, u.PhoneNumber })
            .FirstAsync(cancellationToken);

        return new DepositContractPreviewResponse(
            deposit.Id,
            room.House.Name,
            room.House.AddressLine,
            room.House.Ward,
            room.House.District,
            room.House.Province,
            room.RoomNumber,
            tenant.FullName,
            tenant.PhoneNumber,
            MonthlyRent: deposit.Amount,
            DepositHeld: deposit.Amount,
            deposit.RequestedTermMonths,
            deposit.RequestedStartDate,
            EndDate: deposit.RequestedStartDate.AddMonths(deposit.RequestedTermMonths));
    }
}
