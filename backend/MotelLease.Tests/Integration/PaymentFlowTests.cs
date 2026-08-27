using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Deposits;
using MotelLease.Application.Deposits.Contracts;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Drives the payment group of docs/api-design.md for a deposit. Almost everything worth asserting
/// here is a guard rather than a happy path: that only a signed server-to-server callback moves money
/// state, that the browser's return URL moves nothing, and that a replayed callback is a no-op
/// (docs/domain-rules.md §9.7, §9.8).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PaymentFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public PaymentFlowTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(_postgres.ConnectionString);
        await _app.MigrateAsync();

        // The return endpoint answers with a redirect to the frontend, and the redirect is the
        // assertion. Followed automatically it would be chased into a route this host does not serve.
        _client = _app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();

        return _app.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Checkout_opens_an_attempt_and_pays_for_nothing_by_itself()
    {
        var held = await _app.AcceptedDepositAsync(_client);

        var checkout = await _client.CheckoutAsync(held);

        Assert.Equal(PaymentProvider.VNPay, checkout.Provider);
        Assert.Equal(ListingSetup.MonthlyRent, checkout.Amount);
        Assert.Contains($"vnp_TmnCode={VnPayTestMerchant.TmnCode}", checkout.PaymentUrl);
        Assert.Contains("vnp_SecureHash=", checkout.PaymentUrl);

        // The amount goes to the gateway in the smallest unit.
        Assert.Contains("vnp_Amount=300000000", checkout.PaymentUrl);

        // The attempt cannot outlive the deadline it is paying against.
        Assert.True(checkout.ExpiresAt <= held.Deposit.ExpiresAt);

        var transaction = await TransactionAsync(checkout.TransactionId);

        Assert.Equal(PaymentStatus.Initiated, transaction.Status);
        Assert.False(transaction.SignatureVerified);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task Only_the_ipn_marks_a_deposit_paid()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held);

        // The browser comes back first, carrying a valid signature and a success code. It must not
        // be enough: the user controls this URL.
        var returned = await _client.GetAsync(
            $"/api/v1/payments/vnpay/return{Callback(checkout)}");

        Assert.Equal(HttpStatusCode.Redirect, returned.StatusCode);
        Assert.Contains("outcome=Pending", returned.Headers.Location!.ToString());
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);

        var acknowledgement = await IpnAsync(Callback(checkout));

        Assert.Equal("00", acknowledgement.RspCode);

        var settled = await TransactionAsync(checkout.TransactionId);

        Assert.Equal(PaymentStatus.Succeeded, settled.Status);
        Assert.True(settled.SignatureVerified);
        Assert.Equal(TransactionNo(checkout), settled.ProviderTxnId);
        Assert.NotNull(settled.CompletedAt);

        Assert.Equal(DepositStatus.Paid, await DepositStatusAsync(held.Deposit.Id));

        // A paid deposit still holds the room; it is a lease that would make it Occupied (§9.3).
        Assert.Equal(RoomStatus.Reserved, await RoomStatusAsync(held.Listing.RoomId));

        // Both sides are told (§7).
        Assert.Contains(
            NotificationType.PaymentSucceeded, await NotificationTypesAsync(held.TenantUserId));
        Assert.Contains(
            NotificationType.PaymentSucceeded, await NotificationTypesAsync(held.OwnerUserId));
    }

    [Fact]
    public async Task A_replayed_ipn_changes_nothing()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held);

        var first = await IpnAsync(Callback(checkout));
        var settled = await TransactionAsync(checkout.TransactionId);

        var second = await IpnAsync(Callback(checkout));
        var afterReplay = await TransactionAsync(checkout.TransactionId);

        Assert.Equal("00", first.RspCode);
        Assert.Equal("02", second.RspCode);

        // The row is byte-for-byte what the first call left behind, and the tenant was told once.
        Assert.Equal(settled.CompletedAt, afterReplay.CompletedAt);
        Assert.Equal(PaymentStatus.Succeeded, afterReplay.Status);
        Assert.Equal(DepositStatus.Paid, await DepositStatusAsync(held.Deposit.Id));
        Assert.Single(
            await NotificationTypesAsync(held.TenantUserId),
            type => type == NotificationType.PaymentSucceeded);
    }

    [Fact]
    public async Task An_unsigned_ipn_moves_nothing()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held);

        var acknowledgement = await IpnAsync(VnPayTestMerchant.TamperedIpnQuery(
            checkout.ProviderOrderId, checkout.Amount, TransactionNo(checkout)));

        Assert.Equal("97", acknowledgement.RspCode);
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task An_ipn_for_another_amount_is_refused()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held);

        var acknowledgement = await IpnAsync(VnPayTestMerchant.IpnQuery(
            checkout.ProviderOrderId, checkout.Amount - 1_000_000m, TransactionNo(checkout)));

        Assert.Equal("04", acknowledgement.RspCode);
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task An_ipn_for_an_unknown_order_is_refused()
    {
        var acknowledgement = await IpnAsync(VnPayTestMerchant.IpnQuery(
            "DEPnot-an-order", 3_000_000m, "99999999"));

        Assert.Equal("01", acknowledgement.RspCode);
    }

    [Fact]
    public async Task A_failed_payment_leaves_the_deposit_waiting()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held);

        var acknowledgement = await IpnAsync(VnPayTestMerchant.IpnQuery(
            checkout.ProviderOrderId,
            checkout.Amount,
            TransactionNo(checkout),
            responseCode: "24",
            transactionStatus: "02"));

        Assert.Equal("00", acknowledgement.RspCode);
        Assert.Equal(PaymentStatus.Failed, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task A_payment_that_arrives_after_the_room_was_released_opens_a_refund()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held);

        await ReleaseByDeadlineAsync(held.Deposit.Id);

        Assert.Equal(DepositStatus.Expired, await DepositStatusAsync(held.Deposit.Id));

        var acknowledgement = await IpnAsync(Callback(checkout));

        Assert.Equal("00", acknowledgement.RspCode);

        // The money did arrive, so the transaction says so; pretending otherwise would lose it.
        Assert.Equal(PaymentStatus.Succeeded, (await TransactionAsync(checkout.TransactionId)).Status);

        // The room is somebody else's to take now, and what is owed back is a refund.
        Assert.Equal(DepositStatus.Expired, await DepositStatusAsync(held.Deposit.Id));
        Assert.Equal(RoomStatus.Available, await RoomStatusAsync(held.Listing.RoomId));

        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var refund = await database.RefundRequests.SingleAsync(r => r.DepositId == held.Deposit.Id);

        Assert.Equal(RequestStatus.Pending, refund.Status);
        Assert.Equal(checkout.Amount, refund.Amount);
        Assert.Equal(held.TenantUserId, refund.UserId);
    }

    [Fact]
    public async Task A_request_that_was_not_accepted_cannot_be_paid()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var pending = await _client.RequestDepositAsync(tenant, listing.RoomId);

        var response = await PostCheckoutAsync(tenant, pending.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "error.payment.deposit_not_awaiting_payment", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task Somebody_elses_deposit_cannot_be_paid()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var stranger = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var response = await PostCheckoutAsync(stranger, held.Deposit.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("error.deposit.not_yours", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task Each_side_sees_the_payments_that_concern_them()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held);
        var outsider = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        var tenantView = await ListAsync(held.TenantToken, "/api/v1/payments");
        var ownAccountView = await ListAsync(held.TenantToken, "/api/v1/me/payments");
        var ownerView = await ListAsync(held.Listing.OwnerToken, "/api/v1/payments");
        var strangerView = await ListAsync(outsider, "/api/v1/payments");

        Assert.Equal(checkout.TransactionId, Assert.Single(tenantView.Items).Id);
        Assert.Equal(checkout.TransactionId, Assert.Single(ownAccountView.Items).Id);
        Assert.Equal(checkout.TransactionId, Assert.Single(ownerView.Items).Id);
        Assert.Empty(strangerView.Items);

        // And an id outside that scope reads as not found rather than forbidden.
        var byStranger = await _client.SendAsync(
            HttpMethod.Get, $"/api/v1/payments/{checkout.TransactionId}", outsider);

        Assert.Equal(HttpStatusCode.NotFound, byStranger.StatusCode);
    }

    private Task<HttpResponseMessage> PostCheckoutAsync(string tenantToken, Guid depositId) =>
        _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{depositId}/checkout",
            tenantToken,
            new StartPaymentRequest(PaymentProvider.VNPay));

    /// <summary>The gateway's own id for the attempt, derived so a replay carries the same one.</summary>
    private static string TransactionNo(PaymentCheckoutResponse checkout) =>
        checkout.ProviderOrderId[^12..];

    private static string Callback(PaymentCheckoutResponse checkout) =>
        VnPayTestMerchant.IpnQuery(
            checkout.ProviderOrderId, checkout.Amount, TransactionNo(checkout));

    private async Task<VnPayAcknowledgement> IpnAsync(string query)
    {
        var response = await _client.GetAsync($"/api/v1/payments/vnpay/ipn{query}");

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<VnPayAcknowledgement>();
    }

    private async Task<PagedResponse<PaymentTransactionResponse>> ListAsync(
        string accessToken,
        string path)
    {
        var response = await _client.SendAsync(HttpMethod.Get, path, accessToken);

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<PagedResponse<PaymentTransactionResponse>>();
    }

    private async Task<Domain.Entities.PaymentTransaction> TransactionAsync(Guid id)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.PaymentTransactions.AsNoTracking().FirstAsync(t => t.Id == id);
    }

    private async Task<DepositStatus> DepositStatusAsync(Guid depositId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.Deposits
            .Where(d => d.Id == depositId)
            .Select(d => d.Status)
            .FirstAsync();
    }

    private async Task<RoomStatus> RoomStatusAsync(Guid roomId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.Rooms.Where(r => r.Id == roomId).Select(r => r.Status).FirstAsync();
    }

    private async Task<IReadOnlyList<NotificationType>> NotificationTypesAsync(Guid userId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.Notifications
            .Where(n => n.UserId == userId)
            .Select(n => n.Type)
            .ToListAsync();
    }

    /// <summary>
    /// Brings the payment deadline forward and runs the sweep, which is how a room is released in
    /// production. The endpoint offers no way to shorten the deadline, so the row is edited directly.
    /// </summary>
    private async Task ReleaseByDeadlineAsync(Guid depositId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var deposit = await database.Deposits.FirstAsync(d => d.Id == depositId);

        deposit.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        await database.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<ExpireOverdueDepositsHandler>().HandleAsync();
    }
}
