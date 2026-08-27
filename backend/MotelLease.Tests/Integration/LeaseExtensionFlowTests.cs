using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class LeaseExtensionFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public LeaseExtensionFlowTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(_postgres.ConnectionString);
        await _app.MigrateAsync();
        _client = _app.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return _app.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Tenant_creates_extension_and_owner_approves()
    {
        var held = await _app.PaidDepositAsync(_client);
        var confirmResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken);
        var lease = await confirmResp.ReadAsync<LeaseResponse>();

        var newEndDate = lease.EndDate.AddMonths(6);

        // 1. Tenant creates extension request
        var createResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/extension-requests",
            held.TenantToken,
            new CreateExtensionRequest(lease.Id, newEndDate, "I would like to extend for another 6 months."));

        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var createdExt = await createResp.ReadAsync<ExtensionRequestResponse>();
        Assert.Equal(lease.Id, createdExt.LeaseId);
        Assert.Equal(newEndDate, createdExt.RequestedEndDate);
        Assert.Equal(RequestStatus.Pending, createdExt.Status);

        // 2. Duplicate pending request is rejected
        var dupResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/extension-requests",
            held.TenantToken,
            new CreateExtensionRequest(lease.Id, newEndDate.AddMonths(1), "Another request"));
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

        // 3. Owner lists extension requests
        var listResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/extension-requests?boardingHouseId={held.Listing.HouseId}",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var paged = await listResp.ReadAsync<PagedResponse<ExtensionRequestResponse>>();
        Assert.Contains(paged.Items, e => e.Id == createdExt.Id);

        // 4. Owner approves
        var approveResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/extension-requests/{createdExt.Id}/approve",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approvedExt = await approveResp.ReadAsync<ExtensionRequestResponse>();
        Assert.Equal(RequestStatus.Accepted, approvedExt.Status);

        // 5. Verify lease EndDate is updated
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
        var updatedLease = await db.Leases.FirstAsync(l => l.Id == lease.Id);
        Assert.Equal(newEndDate, updatedLease.EndDate);
    }

    [Fact]
    public async Task Owner_can_reject_extension_request()
    {
        var held = await _app.PaidDepositAsync(_client);
        var confirmResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken);
        var lease = await confirmResp.ReadAsync<LeaseResponse>();

        var newEndDate = lease.EndDate.AddMonths(3);

        var createResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/extension-requests",
            held.TenantToken,
            new CreateExtensionRequest(lease.Id, newEndDate, "Requesting 3 months"));
        var createdExt = await createResp.ReadAsync<ExtensionRequestResponse>();

        var rejectResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/extension-requests/{createdExt.Id}/reject",
            held.Listing.OwnerToken,
            new RejectExtensionRequest("Cannot extend due to upcoming renovation"));

        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejectedExt = await rejectResp.ReadAsync<ExtensionRequestResponse>();
        Assert.Equal(RequestStatus.Rejected, rejectedExt.Status);
        Assert.Equal("Cannot extend due to upcoming renovation", rejectedExt.OwnerNote);
    }
}
