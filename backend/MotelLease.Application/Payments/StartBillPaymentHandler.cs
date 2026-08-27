using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Payments;

/// <summary>
/// POST /payments/bills/{billId}/checkout. The same shape as a deposit checkout — an attempt is
/// opened and a gateway URL handed back, and nothing is paid until the IPN callback says so
/// (docs/domain-rules.md §9.8).
///
/// An overdue bill stays payable: the due date decides whether a reminder goes out, not whether the
/// money is still owed. Only a bill that was never issued, or is already settled, is refused.
/// </summary>
public sealed class StartBillPaymentHandler(
    IAppDbContext database,
    PaymentGateways gateways,
    PaymentSessionWindow window,
    ILocalizer localizer,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<PaymentCheckoutResponse> HandleAsync(
        Guid billId,
        StartPaymentRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var now = time.GetUtcNow();
        var gateway = gateways.For(request.Provider);

        var bill = await database.PaymentBills.FirstOrDefaultAsync(
            b => b.Id == billId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Bill.NotFound);

        // Anybody living under the contract may settle its bill, not only whoever signed it: a
        // co-tenant paying the shared electricity is the ordinary case.
        var livesHere = await database.LeaseTenants.AnyAsync(
            t => t.LeaseId == bill.LeaseId && t.UserId == userId && t.MovedOutAt == null,
            cancellationToken);

        if (!livesHere)
        {
            throw new ForbiddenException(MessageKeys.Bill.NotYours);
        }

        if (bill.Status is not (BillStatus.Issued or BillStatus.Overdue))
        {
            throw new BusinessRuleException(MessageKeys.Bill.NotPayable);
        }

        var label = await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == bill.RoomId)
            .Select(r => new { r.RoomNumber, HouseName = r.BoardingHouse.Name })
            .FirstAsync(cancellationToken);

        var language = await database.Users
            .Where(u => u.Id == userId)
            .Select(u => u.PreferredLanguage)
            .FirstAsync(cancellationToken);

        var transaction = new PaymentTransaction
        {
            UserId = userId,
            Purpose = PaymentPurpose.Rent,
            PaymentBillId = bill.Id,
            Provider = gateway.Provider,
            ProviderOrderId = $"BIL{Guid.CreateVersion7():N}",
            Amount = bill.TotalAmount,
            Status = PaymentStatus.Initiated,
            InitiatedAt = now
        };

        database.PaymentTransactions.Add(transaction);

        await database.SaveChangesAsync(cancellationToken);

        var expiresAt = now + window.Lifetime;

        var url = await gateway.CreatePaymentUrlAsync(new GatewayPaymentRequest(
            transaction.ProviderOrderId,
            transaction.Amount,
            localizer.Get(
                MessageKeys.Payment.BillDescription,
                language,
                bill.Month,
                bill.Year,
                label.RoomNumber),
            expiresAt,
            ipAddress),
            cancellationToken);

        return new PaymentCheckoutResponse(
            transaction.Id,
            transaction.ProviderOrderId,
            transaction.Provider,
            transaction.Amount,
            expiresAt,
            url);
    }
}
