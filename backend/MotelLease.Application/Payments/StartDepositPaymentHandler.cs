using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Payments;

/// <summary>
/// POST /deposits/{id}/checkout. Opens a payment attempt for an accepted deposit and hands back the
/// gateway URL. Nothing about money moves here — the row is Initiated and stays that way until the
/// IPN callback says otherwise (docs/domain-rules.md §9.8).
/// </summary>
public sealed class StartDepositPaymentHandler(
    IAppDbContext database,
    PaymentGateways gateways,
    PaymentSessionWindow window,
    ILocalizer localizer,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<PaymentCheckoutResponse> HandleAsync(
        Guid depositId,
        StartPaymentRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var now = time.GetUtcNow();
        var gateway = gateways.For(request.Provider);

        var deposit = await database.Deposits.FirstOrDefaultAsync(
            d => d.Id == depositId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Deposit.NotFound);

        if (deposit.UserId != userId)
        {
            throw new ForbiddenException(MessageKeys.Deposit.NotYours);
        }

        // Only an accepted request is payable: a pending one has not been agreed to, and anything
        // past that has either been paid already or stopped holding the room.
        if (deposit.Status != DepositStatus.Accepted)
        {
            throw new BusinessRuleException(MessageKeys.Payment.DepositNotAwaitingPayment);
        }

        if (deposit.ExpiresAt is null || deposit.ExpiresAt <= now)
        {
            throw new BusinessRuleException(MessageKeys.Payment.DeadlinePassed);
        }

        // The attempt cannot outlive the deadline it is paying against, and does not need the whole
        // of it either. Whichever comes first wins.
        var expiresAt = Min(now + window.Lifetime, deposit.ExpiresAt.Value);

        var label = await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == deposit.RoomId)
            .Select(r => new { r.RoomNumber, HouseName = r.BoardingHouse.Name })
            .FirstAsync(cancellationToken);

        var language = await database.Users
            .Where(u => u.Id == userId)
            .Select(u => u.PreferredLanguage)
            .FirstAsync(cancellationToken);

        var transaction = new PaymentTransaction
        {
            UserId = userId,
            Purpose = PaymentPurpose.Deposit,
            DepositId = deposit.Id,
            Provider = gateway.Provider,
            // Prefixed so a row is recognisable in a gateway's own dashboard without a lookup.
            ProviderOrderId = $"DEP{Guid.CreateVersion7():N}",
            Amount = deposit.Amount,
            Status = PaymentStatus.Initiated,
            InitiatedAt = now
        };

        database.PaymentTransactions.Add(transaction);

        await database.SaveChangesAsync(cancellationToken);

        var url = gateway.BuildPaymentUrl(new GatewayPaymentRequest(
            transaction.ProviderOrderId,
            transaction.Amount,
            localizer.Get(
                MessageKeys.Payment.DepositDescription,
                language,
                label.RoomNumber,
                label.HouseName),
            expiresAt,
            ipAddress));

        return new PaymentCheckoutResponse(
            transaction.Id,
            transaction.ProviderOrderId,
            transaction.Provider,
            transaction.Amount,
            expiresAt,
            url);
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) =>
        left < right ? left : right;
}
