using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.SavedListings.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class SavedListingsFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public SavedListingsFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Tenant_can_save_list_and_remove_saved_listings()
    {
        var ownerToken = await _client.RegisterAsync(_app.Emails, UserRole.Owner);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var houseResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                "Trọ Xanh Cầu Giấy",
                null,
                BoardingHouseType.MiniHouse,
                "10 Tran Thai Tong",
                "Dich Vong",
                "Cau Giay",
                "Ha Noi",
                21.0333m,
                105.7890m));
        var house = await houseResp.ReadAsync<BoardingHouseDetailResponse>();

        // Publish house
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var h = await db.BoardingHouses.FirstAsync(x => x.Id == house.Id);
            h.ListingStatus = ListingStatus.Published;
            await db.SaveChangesAsync();
        }

        // 1. Anonymous cannot access saved listings
        var anonResp = await _client.GetAsync("/api/v1/me/saved-listings");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResp.StatusCode);

        // 2. Tenant saves listing
        var saveResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/me/saved-listings",
            tenantToken,
            new SaveListingRequest(house.Id));
        Assert.Equal(HttpStatusCode.OK, saveResp.StatusCode);
        var saved = await saveResp.ReadAsync<SavedListingResponse>();
        Assert.Equal(house.Id, saved.BoardingHouseId);
        Assert.Equal("Trọ Xanh Cầu Giấy", saved.BoardingHouse.Name);

        // 3. Duplicate save is idempotent
        var dupResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/me/saved-listings",
            tenantToken,
            new SaveListingRequest(house.Id));
        Assert.Equal(HttpStatusCode.OK, dupResp.StatusCode);

        // 4. List saved listings
        var listResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/me/saved-listings",
            tenantToken);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var paged = await listResp.ReadAsync<PagedResponse<SavedListingResponse>>();
        Assert.Contains(paged.Items, s => s.BoardingHouseId == house.Id);

        // 5. Remove saved listing
        var removeResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/me/saved-listings/{house.Id}",
            tenantToken);
        Assert.Equal(HttpStatusCode.NoContent, removeResp.StatusCode);

        // 6. List is now empty
        var listAfterResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/me/saved-listings",
            tenantToken);
        var pagedAfter = await listAfterResp.ReadAsync<PagedResponse<SavedListingResponse>>();
        Assert.DoesNotContain(pagedAfter.Items, s => s.BoardingHouseId == house.Id);
    }
}
