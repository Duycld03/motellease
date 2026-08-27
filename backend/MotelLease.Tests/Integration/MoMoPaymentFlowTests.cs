using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// The MoMo half of the payment group. Same rules as VNPay — only a signed server-to-server callback
/// settles anything, a replay changes nothing (docs/domain-rules.md §9.7, §9.8) — reached over a
/// protocol that agrees with VNPay's on almost nothing: the payment URL is fetched from MoMo rather
/// than assembled, the callback arrives as a JSON body rather than a query string, the amount is plain
/// VND, and the acknowledgement is an HTTP status rather than a code in a body.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class MoMoPaymentFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public MoMoPaymentFlowTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(_postgres.ConnectionString);
        await _app.MigrateAsync();
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
    public async Task Checkout_asks_momo_for_a_payment_url()
    {
        var held = await _app.AcceptedDepositAsync(_client);

        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);

        Assert.Equal(PaymentProvider.MoMo, checkout.Provider);
        Assert.Equal(_app.MoMoApi.PayUrl, checkout.PaymentUrl);

        var sent = _app.MoMoApi.LastRequest;

        Assert.Equal(MoMoTestMerchant.PartnerCode, sent.GetProperty("partnerCode").GetString());
        Assert.Equal(checkout.ProviderOrderId, sent.GetProperty("orderId").GetString());

        // Plain VND, unlike VNPay's smallest unit.
        Assert.Equal("3000000", sent.GetProperty("amount").GetString());

        // MoMo is told where to call back, and it is our own IPN endpoint.
        Assert.EndsWith(
            "/api/v1/payments/momo/ipn", sent.GetProperty("ipnUrl").GetString()!);
        Assert.Equal(64, sent.GetProperty("signature").GetString()!.Length);

        // Opening the attempt settles nothing.
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task A_signed_callback_marks_the_deposit_paid()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);

        var acknowledged = await IpnAsync(
            MoMoTestMerchant.Callback(checkout.ProviderOrderId, checkout.Amount, TransId(checkout)));

        // MoMo reads the status, not a body.
        Assert.Equal(HttpStatusCode.NoContent, acknowledged.StatusCode);

        var settled = await TransactionAsync(checkout.TransactionId);

        Assert.Equal(PaymentStatus.Succeeded, settled.Status);
        Assert.True(settled.SignatureVerified);
        Assert.Equal(TransId(checkout), settled.ProviderTxnId);
        Assert.Equal(DepositStatus.Paid, await DepositStatusAsync(held.Deposit.Id));
        Assert.Equal(RoomStatus.Reserved, await RoomStatusAsync(held.Listing.RoomId));
    }

    [Fact]
    public async Task A_replayed_callback_changes_nothing()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);
        var callback = MoMoTestMerchant.Callback(
            checkout.ProviderOrderId, checkout.Amount, TransId(checkout));

        var first = await IpnAsync(callback);
        var settled = await TransactionAsync(checkout.TransactionId);

        var second = await IpnAsync(callback);
        var afterReplay = await TransactionAsync(checkout.TransactionId);

        // Both acknowledged, because a retry MoMo keeps resending is worse than one it stops.
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        Assert.Equal(settled.CompletedAt, afterReplay.CompletedAt);
        Assert.Equal(DepositStatus.Paid, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task An_unsigned_callback_moves_nothing()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);

        var acknowledged = await IpnAsync(MoMoTestMerchant.TamperedCallback(
            checkout.ProviderOrderId, checkout.Amount, TransId(checkout)));

        Assert.Equal(HttpStatusCode.BadRequest, acknowledged.StatusCode);
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task A_callback_for_another_amount_is_refused()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);

        var acknowledged = await IpnAsync(MoMoTestMerchant.Callback(
            checkout.ProviderOrderId, checkout.Amount - 500_000m, TransId(checkout)));

        Assert.Equal(HttpStatusCode.BadRequest, acknowledged.StatusCode);
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
    }

    [Fact]
    public async Task A_refused_payment_leaves_the_deposit_waiting()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);

        var acknowledged = await IpnAsync(MoMoTestMerchant.Callback(
            checkout.ProviderOrderId, checkout.Amount, TransId(checkout), resultCode: 1006));

        Assert.Equal(HttpStatusCode.NoContent, acknowledged.StatusCode);
        Assert.Equal(PaymentStatus.Failed, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    [Fact]
    public async Task A_callback_for_an_unknown_order_is_refused()
    {
        var acknowledged = await IpnAsync(
            MoMoTestMerchant.Callback("DEPnot-an-order", 3_000_000m, "999999999999"));

        Assert.Equal(HttpStatusCode.BadRequest, acknowledged.StatusCode);
    }

    [Fact]
    public async Task An_attempt_opened_at_momo_cannot_be_settled_through_vnpay()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);

        // Correctly signed for VNPay, naming an order MoMo is holding. Holding one gateway's secret
        // says nothing about the right to settle the other's attempts.
        var byVnPay = await _client.GetAsync(
            $"/api/v1/payments/vnpay/ipn{VnPayTestMerchant.IpnQuery(
                checkout.ProviderOrderId, checkout.Amount, TransId(checkout))}");

        var acknowledgement = await byVnPay.ReadAsync<VnPayAcknowledgement>();

        Assert.Equal("01", acknowledgement.RspCode);
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));

        // And a payload signed the VNPay way is not a MoMo callback either: the digest is a different
        // algorithm over a different string.
        var byMoMo = await _client.PostAsJsonAsync(
            "/api/v1/payments/momo/ipn",
            new Dictionary<string, object>
            {
                ["orderId"] = checkout.ProviderOrderId,
                ["amount"] = "3000000",
                ["transId"] = TransId(checkout),
                ["resultCode"] = "0",
                ["signature"] = new string('a', 128)
            });

        Assert.Equal(HttpStatusCode.BadRequest, byMoMo.StatusCode);
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
    }

    [Fact]
    public async Task Momo_refusing_to_open_a_payment_is_reported_as_such()
    {
        _app.MoMoApi.ResultCode = 1000;

        var held = await _app.AcceptedDepositAsync(_client);

        var response = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/checkout",
            held.TenantToken,
            new StartPaymentRequest(PaymentProvider.MoMo));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.payment.gateway_rejected", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task The_return_url_settles_nothing()
    {
        var held = await _app.AcceptedDepositAsync(_client);
        var checkout = await _client.CheckoutAsync(held, PaymentProvider.MoMo);

        var callback = MoMoTestMerchant.Callback(
            checkout.ProviderOrderId, checkout.Amount, TransId(checkout));

        var query = string.Join(
            '&', callback.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value.ToString()!)}"));

        var returned = await _client.GetAsync($"/api/v1/payments/momo/return?{query}");

        Assert.Equal(HttpStatusCode.Redirect, returned.StatusCode);
        Assert.Contains("outcome=Pending", returned.Headers.Location!.ToString());
        Assert.Equal(PaymentStatus.Initiated, (await TransactionAsync(checkout.TransactionId)).Status);
        Assert.Equal(DepositStatus.Accepted, await DepositStatusAsync(held.Deposit.Id));
    }

    /// <summary>
    /// MoMo's own id for the transaction, derived from the order so a replay carries the same one and
    /// two tests never collide on the unique index that makes §9.7 hold.
    /// </summary>
    private static string TransId(PaymentCheckoutResponse checkout) =>
        checkout.ProviderOrderId[^12..];

    private Task<HttpResponseMessage> IpnAsync(Dictionary<string, object> callback) =>
        _client.PostAsJsonAsync("/api/v1/payments/momo/ipn", callback);

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
}
