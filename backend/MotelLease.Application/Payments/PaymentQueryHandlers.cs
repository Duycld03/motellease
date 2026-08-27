using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Payments;

internal static class PaymentRules
{
    internal static IQueryable<PaymentTransactionResponse> Project(
        IQueryable<PaymentTransaction> query) =>
        query.Select(t => new PaymentTransactionResponse(
            t.Id,
            t.UserId,
            t.Purpose,
            t.Provider,
            t.ProviderOrderId,
            t.ProviderTxnId,
            t.Amount,
            t.Status,
            t.SignatureVerified,
            t.DepositId,
            t.PaymentBillId,
            t.RefundRequestId,
            t.InitiatedAt,
            t.CompletedAt));

    /// <summary>
    /// The transactions the caller is entitled to see. A tenant sees their own payments, an owner or
    /// staff member the ones made against the properties they run, and an admin all of them — the
    /// history is where a dispute is settled, so nobody sees a row that is not theirs to settle.
    /// </summary>
    internal static IQueryable<PaymentTransaction> Visible(
        IAppDbContext database,
        BoardingHouseAccess access,
        ICurrentUser currentUser)
    {
        var userId = currentUser.RequireUserId();

        return currentUser.Role switch
        {
            UserRole.Admin => database.PaymentTransactions,
            UserRole.Tenant => database.PaymentTransactions.Where(t => t.UserId == userId),
            _ => database.PaymentTransactions.Where(t =>
                database.Deposits.Any(d =>
                    d.Id == t.DepositId
                    && access.Managed().Any(b => b.Id == d.Room.BoardingHouseId))
                || database.PaymentBills.Any(bill =>
                    bill.Id == t.PaymentBillId
                    && access.Managed().Any(b => b.Id == bill.Room.BoardingHouseId)))
        };
    }
}

/// <summary>
/// GET /payments and GET /me/payments. One query, scoped by role, because a transaction history is
/// the same list read from two sides (docs/api-design.md).
/// </summary>
public sealed class ListPaymentsHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<PaymentTransactionResponse>> HandleAsync(
        PaymentStatus? status,
        PaymentPurpose? purpose,
        bool ownOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = PaymentRules.Visible(database, access, currentUser);

        if (ownOnly)
        {
            var userId = currentUser.RequireUserId();

            query = query.Where(t => t.UserId == userId);
        }

        if (status is { } wantedStatus)
        {
            query = query.Where(t => t.Status == wantedStatus);
        }

        if (purpose is { } wantedPurpose)
        {
            query = query.Where(t => t.Purpose == wantedPurpose);
        }

        return await Paged.FromAsync(
            PaymentRules.Project(query.AsNoTracking().OrderByDescending(t => t.InitiatedAt)),
            page,
            pageSize,
            cancellationToken);
    }
}

/// <summary>
/// GET /payments/{id}. Scoped the same way as the list, so an id belonging to somebody else reads as
/// not found rather than forbidden — answering "not yours" would confirm the row exists.
/// </summary>
public sealed class GetPaymentHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<PaymentTransactionResponse> HandleAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default) =>
        await PaymentRules
            .Project(PaymentRules
                .Visible(database, access, currentUser)
                .AsNoTracking()
                .Where(t => t.Id == transactionId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Payment.NotFound);
}

/// <summary>
/// The browser's return from the gateway. It reads the signature only to decide which page to show;
/// it writes nothing, because the user controls this URL and a payment confirmed from it would be a
/// payment anybody could claim (CLAUDE.md, Hard prohibitions). The IPN callback is what settles the
/// money, and it may not have arrived yet — hence <see cref="PaymentReturn.Pending"/>.
/// </summary>
public sealed class ReadPaymentReturnHandler(IAppDbContext database, PaymentGateways gateways)
{
    public async Task<PaymentReturn> HandleAsync(
        PaymentProvider provider,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken = default)
    {
        var callback = gateways.For(provider).ReadCallback(fields);

        if (!callback.SignatureVerified || string.IsNullOrWhiteSpace(callback.OrderId))
        {
            return new PaymentReturn(PaymentReturnOutcome.Invalid, null, null);
        }

        var transaction = await database.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.ProviderOrderId == callback.OrderId)
            .Select(t => new { t.Id, t.Status, t.DepositId })
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
        {
            return new PaymentReturn(PaymentReturnOutcome.Invalid, null, null);
        }

        var outcome = transaction.Status switch
        {
            PaymentStatus.Succeeded => PaymentReturnOutcome.Succeeded,
            PaymentStatus.Failed => PaymentReturnOutcome.Failed,
            // Signed, and the gateway says it went through, but our own record has not been settled
            // by the callback yet. Telling the tenant "paid" here would be trusting the browser.
            _ => callback.Succeeded
                ? PaymentReturnOutcome.Pending
                : PaymentReturnOutcome.Failed
        };

        return new PaymentReturn(outcome, transaction.Id, transaction.DepositId);
    }
}

public enum PaymentReturnOutcome
{
    /// <summary>Unsigned, or naming an attempt that does not exist. Nothing is shown about it.</summary>
    Invalid,

    /// <summary>The IPN has not settled the row yet; the client polls or waits.</summary>
    Pending,

    Succeeded,
    Failed
}

public sealed record PaymentReturn(
    PaymentReturnOutcome Outcome,
    Guid? TransactionId,
    Guid? DepositId);
