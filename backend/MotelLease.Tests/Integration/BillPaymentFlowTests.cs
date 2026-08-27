using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Paying a monthly bill. The rule under test is the same one the deposit payments prove — a bill
/// reaches Paid only from a signed server-to-server callback (docs/domain-rules.md §9.8) — plus who
/// is allowed to settle one: anybody living under the contract, not only whoever signed it.
///
/// The bill itself is seeded. Issuing one is a later feature group, and the payment path does not wait
/// on it.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class BillPaymentFlowTests : IAsyncLifetime
{
    private const decimal BillTotal = 3_450_000m;

    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public BillPaymentFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task A_bill_is_settled_by_the_ipn_and_not_by_the_checkout()
    {
        var signed = await _app.SignedLeaseAsync(_client);
        var billId = await _app.IssueBillAsync(
            signed.Lease.Id, signed.Held.Listing.RoomId, BillTotal);

        var checkout = await CheckoutAsync(signed, billId);

        Assert.Equal(PaymentPurpose.Rent, await PurposeAsync(checkout.TransactionId));
        Assert.Equal(BillTotal, checkout.Amount);
        Assert.Contains("vnp_Amount=345000000", checkout.PaymentUrl);

        // Opening the attempt settles nothing.
        Assert.Equal(BillStatus.Issued, (await BillAsync(billId)).Status);

        var acknowledgement = await IpnAsync(checkout);

        Assert.Equal("00", acknowledgement.RspCode);

        var settled = await BillAsync(billId);

        Assert.Equal(BillStatus.Paid, settled.Status);
        Assert.NotNull(settled.PaidAt);

        // Both sides are told (§7).
        Assert.Contains(
            NotificationType.PaymentSucceeded,
            await NotificationTypesAsync(signed.Held.TenantUserId));
        Assert.Contains(
            NotificationType.PaymentSucceeded,
            await NotificationTypesAsync(signed.Held.OwnerUserId));
    }

    [Fact]
    public async Task A_replayed_bill_callback_leaves_the_settled_bill_alone()
    {
        var signed = await _app.SignedLeaseAsync(_client);
        var billId = await _app.IssueBillAsync(
            signed.Lease.Id, signed.Held.Listing.RoomId, BillTotal);

        var checkout = await CheckoutAsync(signed, billId);

        var first = await IpnAsync(checkout);
        var settled = await BillAsync(billId);

        var second = await IpnAsync(checkout);
        var afterReplay = await BillAsync(billId);

        Assert.Equal("00", first.RspCode);
        Assert.Equal("02", second.RspCode);
        Assert.Equal(settled.PaidAt, afterReplay.PaidAt);
        Assert.Equal(BillStatus.Paid, afterReplay.Status);
    }

    [Fact]
    public async Task An_unissued_bill_cannot_be_paid()
    {
        var signed = await _app.SignedLeaseAsync(_client);
        var billId = await _app.IssueBillAsync(
            signed.Lease.Id, signed.Held.Listing.RoomId, BillTotal, BillStatus.Draft);

        var response = await PostCheckoutAsync(signed.Held.TenantToken, billId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("error.bill.not_payable", await response.ReadCodeAsync());
    }

    [Fact]
    public async Task An_overdue_bill_is_still_payable()
    {
        var signed = await _app.SignedLeaseAsync(_client);
        var billId = await _app.IssueBillAsync(
            signed.Lease.Id, signed.Held.Listing.RoomId, BillTotal, BillStatus.Overdue);

        var checkout = await CheckoutAsync(signed, billId);
        var acknowledgement = await IpnAsync(checkout);

        Assert.Equal("00", acknowledgement.RspCode);
        Assert.Equal(BillStatus.Paid, (await BillAsync(billId)).Status);
    }

    [Fact]
    public async Task Somebody_who_does_not_live_there_cannot_pay_the_bill()
    {
        var signed = await _app.SignedLeaseAsync(_client);
        var billId = await _app.IssueBillAsync(
            signed.Lease.Id, signed.Held.Listing.RoomId, BillTotal);
        var stranger = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var response = await PostCheckoutAsync(stranger, billId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("error.bill.not_yours", await response.ReadCodeAsync());
        Assert.Equal(BillStatus.Issued, (await BillAsync(billId)).Status);
    }

    [Fact]
    public async Task The_property_side_sees_the_rent_payment_in_its_history()
    {
        var signed = await _app.SignedLeaseAsync(_client);
        var billId = await _app.IssueBillAsync(
            signed.Lease.Id, signed.Held.Listing.RoomId, BillTotal);

        var checkout = await CheckoutAsync(signed, billId);

        var response = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/payments?purpose=Rent",
            signed.Held.Listing.OwnerToken);

        response.EnsureSuccessStatusCode();

        var history = await response
            .ReadAsync<Application.Common.Contracts.PagedResponse<PaymentTransactionResponse>>();

        Assert.Equal(checkout.TransactionId, Assert.Single(history.Items).Id);
    }

    private async Task<PaymentCheckoutResponse> CheckoutAsync(SignedLease signed, Guid billId)
    {
        var response = await PostCheckoutAsync(signed.Held.TenantToken, billId);

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<PaymentCheckoutResponse>();
    }

    private Task<HttpResponseMessage> PostCheckoutAsync(string tenantToken, Guid billId) =>
        _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/payments/bills/{billId}/checkout",
            tenantToken,
            new StartPaymentRequest(PaymentProvider.VNPay));

    private async Task<VnPayAcknowledgement> IpnAsync(PaymentCheckoutResponse checkout)
    {
        var query = VnPayTestMerchant.IpnQuery(
            checkout.ProviderOrderId, checkout.Amount, checkout.ProviderOrderId[^12..]);

        var response = await _client.GetAsync($"/api/v1/payments/vnpay/ipn{query}");

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<VnPayAcknowledgement>();
    }

    private async Task<Domain.Entities.PaymentBill> BillAsync(Guid billId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.PaymentBills.AsNoTracking().FirstAsync(b => b.Id == billId);
    }

    private async Task<PaymentPurpose> PurposeAsync(Guid transactionId)
    {
        using var scope = _app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        return await database.PaymentTransactions
            .Where(t => t.Id == transactionId)
            .Select(t => t.Purpose)
            .FirstAsync();
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
}
