using MotelLease.Application.Deposits.Contracts;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Tests.Integration;

/// <summary>
/// A room held by a deposit, and the steps that take one from requested to paid. Shared because every
/// group downstream of the deposit — payments, leases, bills — starts from one of these states, and
/// each step goes through the real endpoint so the tests keep exercising the shipped code.
/// </summary>
internal static class DepositSetup
{
    /// <summary>A published room with an accepted deposit: the state a payment starts from.</summary>
    internal static async Task<HeldRoom> AcceptedDepositAsync(
        this MotelLeaseAppFactory app,
        HttpClient client)
    {
        var listing = await app.PublishedListingAsync(client);
        var tenant = await client.RegisterAsync(app.Emails, UserRole.Tenant);
        var requested = await client.RequestDepositAsync(tenant, listing.RoomId);

        var approved = await client.SendAsync(
            HttpMethod.Put, $"/api/v1/deposits/{requested.Id}/approve", listing.OwnerToken);

        approved.EnsureSuccessStatusCode();

        return new HeldRoom(
            listing,
            tenant,
            requested.TenantUserId,
            await client.UserIdAsync(listing.OwnerToken),
            await approved.ReadAsync<DepositResponse>());
    }

    /// <summary>
    /// The same, carried through checkout and a signed IPN callback so the deposit is Paid. The
    /// callback is the only way to get there, which is the point of §9.8.
    /// </summary>
    internal static async Task<HeldRoom> PaidDepositAsync(
        this MotelLeaseAppFactory app,
        HttpClient client)
    {
        var held = await app.AcceptedDepositAsync(client);
        var checkout = await client.CheckoutAsync(held);

        var acknowledged = await client.GetAsync(
            $"/api/v1/payments/vnpay/ipn{VnPayTestMerchant.IpnQuery(
                checkout.ProviderOrderId, checkout.Amount, checkout.ProviderOrderId[^12..])}");

        acknowledged.EnsureSuccessStatusCode();

        return held;
    }

    /// <summary>
    /// Carried one step further: the paid deposit is signed into a lease, which is where a monthly
    /// bill can exist at all.
    /// </summary>
    internal static async Task<SignedLease> SignedLeaseAsync(
        this MotelLeaseAppFactory app,
        HttpClient client)
    {
        var held = await app.PaidDepositAsync(client);

        var response = await client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken);

        response.EnsureSuccessStatusCode();

        return new SignedLease(held, await response.ReadAsync<LeaseResponse>());
    }

    internal static async Task<DepositResponse> RequestDepositAsync(
        this HttpClient client,
        string tenantToken,
        Guid roomId,
        int termMonths = 12)
    {
        var response = await client.SendAsync(
            HttpMethod.Post,
            "/api/v1/deposits",
            tenantToken,
            new RequestDepositRequest(
                roomId,
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3),
                termMonths));

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<DepositResponse>();
    }

    internal static async Task<PaymentCheckoutResponse> CheckoutAsync(
        this HttpClient client,
        HeldRoom held)
    {
        var response = await client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/checkout",
            held.TenantToken,
            new StartPaymentRequest(PaymentProvider.VNPay));

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<PaymentCheckoutResponse>();
    }
}

internal sealed record HeldRoom(
    Listing Listing,
    string TenantToken,
    Guid TenantUserId,
    Guid OwnerUserId,
    DepositResponse Deposit);

internal sealed record SignedLease(HeldRoom Held, LeaseResponse Lease);
