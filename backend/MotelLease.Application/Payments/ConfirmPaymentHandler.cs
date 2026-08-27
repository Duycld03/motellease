using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Notifications;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Payments;

/// <summary>
/// The IPN callback, and the only place in the application that moves money state
/// (docs/domain-rules.md §9.7, §9.8). A browser return URL never reaches this code: the user controls
/// the URL they land on, so only a server-to-server callback carrying a signature made with our own
/// secret is allowed to mark anything paid.
///
/// Everything happens inside one transaction and the whole thing is idempotent — a gateway retries an
/// IPN until it is acknowledged, so the second delivery of a payment must change nothing at all.
/// </summary>
public sealed class ConfirmPaymentHandler(
    IAppDbContext database,
    PaymentGateways gateways,
    NotificationDispatcher notifications,
    TimeProvider time)
{
    public async Task<PaymentConfirmation> HandleAsync(
        PaymentProvider provider,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken = default)
    {
        var callback = gateways.For(provider).ReadCallback(fields);

        // Checked before anything is read from the database: an unsigned payload proves nothing
        // about who sent it, so it does not get to name a row.
        if (!callback.SignatureVerified)
        {
            return PaymentConfirmation.InvalidSignature;
        }

        if (string.IsNullOrWhiteSpace(callback.OrderId))
        {
            return PaymentConfirmation.OrderNotFound;
        }

        await using var scope = await database.BeginTransactionAsync(cancellationToken);

        var transaction = await database.PaymentTransactions.FirstOrDefaultAsync(
            t => t.ProviderOrderId == callback.OrderId, cancellationToken);

        if (transaction is null)
        {
            return PaymentConfirmation.OrderNotFound;
        }

        // An attempt opened at one gateway is not settleable through another's endpoint, however well
        // signed the payload is. Reported as not found rather than as a mismatch: the caller proved it
        // holds one provider's secret, which says nothing about its right to know about the other's
        // orders.
        if (transaction.Provider != provider)
        {
            return PaymentConfirmation.OrderNotFound;
        }

        // The replay guard. The unique index on ProviderTxnId is the authority (§9.7); this check
        // exists so a retry is acknowledged rather than answered with a constraint violation.
        if (transaction.Status is PaymentStatus.Succeeded or PaymentStatus.Failed
            || await IsTxnIdRecordedAsync(callback.ProviderTxnId, transaction.Id, cancellationToken))
        {
            return PaymentConfirmation.AlreadyConfirmed;
        }

        // A payment for a different amount than the one agreed is not this payment.
        if (callback.Amount != transaction.Amount)
        {
            return PaymentConfirmation.InvalidAmount;
        }

        transaction.RawCallbackPayload = callback.RawPayload;
        transaction.SignatureVerified = true;
        transaction.ProviderTxnId = callback.ProviderTxnId;
        transaction.CompletedAt = time.GetUtcNow();
        transaction.Status = callback.Succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed;

        if (callback.Succeeded)
        {
            await CreditAsync(transaction, cancellationToken);
        }

        await database.SaveChangesAsync(cancellationToken);

        await scope.CommitAsync(cancellationToken);

        // After the commit, so nothing is announced that could still roll back.
        await notifications.DeliverAsync(cancellationToken);

        return PaymentConfirmation.Confirmed;
    }

    /// <summary>
    /// Applies a successful payment to whatever it was for. Each purpose is one branch, and a purpose
    /// with nothing to credit yet is left alone rather than guessed at.
    /// </summary>
    private Task CreditAsync(PaymentTransaction transaction, CancellationToken cancellationToken) =>
        transaction.Purpose switch
        {
            PaymentPurpose.Deposit when transaction.DepositId is not null =>
                CreditDepositAsync(transaction, cancellationToken),
            PaymentPurpose.Rent when transaction.PaymentBillId is not null =>
                CreditBillAsync(transaction, cancellationToken),
            _ => Task.CompletedTask
        };

    private async Task CreditDepositAsync(
        PaymentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var deposit = await database.Deposits.FirstAsync(
            d => d.Id == transaction.DepositId, cancellationToken);

        // The money arrived after the room stopped being held — the sweep released it, or the tenant
        // withdrew. The payment is real and stays recorded as such; what is owed back is a refund,
        // opened here so it cannot be quietly forgotten.
        if (deposit.Status != DepositStatus.Accepted)
        {
            database.RefundRequests.Add(new RefundRequest
            {
                DepositId = deposit.Id,
                UserId = deposit.UserId,
                Amount = transaction.Amount,
                Status = RequestStatus.Pending,
                Reason = MessageKeys.Payment.RefundReasonPaidAfterDeadline
            });

            return;
        }

        deposit.Status = DepositStatus.Paid;

        await NotifyPaidAsync(
            deposit.RoomId,
            deposit.UserId,
            transaction,
            linkUrl: $"/deposits/{deposit.Id}",
            cancellationToken);
    }

    private async Task CreditBillAsync(
        PaymentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var bill = await database.PaymentBills.FirstAsync(
            b => b.Id == transaction.PaymentBillId, cancellationToken);

        // A bill reaches Paid only from here, and only once (§9.8). One already settled is left as it
        // is; the money is recorded on the transaction either way, and a double payment is a refund
        // question rather than a reason to rewrite a settled invoice.
        if (bill.Status is not (BillStatus.Issued or BillStatus.Overdue))
        {
            return;
        }

        bill.Status = BillStatus.Paid;
        bill.PaidAt = transaction.CompletedAt;

        await NotifyPaidAsync(
            bill.RoomId,
            transaction.UserId,
            transaction,
            linkUrl: $"/bills/{bill.Id}",
            cancellationToken);
    }

    /// <summary>
    /// Both sides are told (docs/domain-rules.md §7): the payer that the money went through, and the
    /// owner that it arrived.
    /// </summary>
    private async Task NotifyPaidAsync(
        Guid roomId,
        Guid payerUserId,
        PaymentTransaction transaction,
        string linkUrl,
        CancellationToken cancellationToken)
    {
        var label = await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == roomId)
            .Select(r => new
            {
                r.RoomNumber,
                HouseName = r.BoardingHouse.Name,
                r.BoardingHouse.OwnerUserId
            })
            .FirstAsync(cancellationToken);

        var payload = new
        {
            transactionId = transaction.Id,
            purpose = transaction.Purpose.ToString(),
            roomNumber = label.RoomNumber,
            boardingHouseName = label.HouseName,
            amount = transaction.Amount,
            provider = transaction.Provider.ToString()
        };

        foreach (var recipient in new[] { payerUserId, label.OwnerUserId }.Distinct())
        {
            notifications.Queue(
                recipient,
                NotificationType.PaymentSucceeded,
                payload,
                linkUrl);
        }
    }

    private async Task<bool> IsTxnIdRecordedAsync(
        string? providerTxnId,
        Guid exceptTransactionId,
        CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(providerTxnId)
        && await database.PaymentTransactions.AnyAsync(
            t => t.ProviderTxnId == providerTxnId && t.Id != exceptTransactionId,
            cancellationToken);
}
