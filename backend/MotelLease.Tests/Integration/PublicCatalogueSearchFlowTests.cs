using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Catalogue.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Rooms.Contracts;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class PublicCatalogueSearchFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public PublicCatalogueSearchFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Search_returns_published_listings_and_filters_correctly()
    {
        var ownerToken = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        // Create House 1 in Cầu Giấy, Hanoi (Published)
        var house1Resp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                "Nha Tro Cau Giay Luxury",
                "Phòng trọ cao cấp gần ĐH Quốc Gia",
                BoardingHouseType.MiniHouse,
                "123 Xuan Thuy",
                "Dich Vong Hau",
                "Cau Giay",
                "Ha Noi",
                21.0378m,
                105.7825m));
        house1Resp.EnsureSuccessStatusCode();
        var house1 = await house1Resp.ReadAsync<BoardingHouseDetailResponse>();

        // Add room type for House 1 (price 4,500,000, max occupants 1 for MiniHouse)
        var rt1Resp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{house1.Id}/room-types",
            ownerToken,
            new SaveRoomTypeRequest("Studio VIP", 4500000, 25, 1, "Full do", []));
        rt1Resp.EnsureSuccessStatusCode();
        var rt1 = await rt1Resp.ReadAsync<RoomTypeResponse>();

        // Add room for House 1
        var roomResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{house1.Id}/rooms",
            ownerToken,
            new SaveRoomRequest(rt1.Id, "101", "Phong 101"));
        roomResp.EnsureSuccessStatusCode();

        // Create House 2 in Hai Ba Trung, Hanoi (Draft - not published)
        var house2Resp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                "Nha Tro Bach Khoa",
                "Phong tro sinh vien",
                BoardingHouseType.Traditional,
                "45 Ta Quang Buu",
                "Bach Khoa",
                "Hai Ba Trung",
                "Ha Noi",
                21.0045m,
                105.8435m));
        house2Resp.EnsureSuccessStatusCode();
        var house2 = await house2Resp.ReadAsync<BoardingHouseDetailResponse>();

        // Publish House 1 directly in DB to simulate approved listing
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var h1 = await db.BoardingHouses.FirstAsync(x => x.Id == house1.Id);
            h1.ListingStatus = ListingStatus.Published;
            h1.Rating = 4.8m;
            h1.ReviewCount = 5;
            await db.SaveChangesAsync();
        }

        // 1. Anonymous search returns House 1, but excludes draft House 2
        var searchResp = await _client.GetAsync("/api/v1/boarding-houses?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);
        var paged = await searchResp.ReadAsync<PagedResponse<PublicBoardingHouseCardResponse>>();
        Assert.Contains(paged.Items, h => h.Id == house1.Id);
        Assert.DoesNotContain(paged.Items, h => h.Id == house2.Id);

        // 2. Keyword filter
        var kwResp = await _client.GetAsync("/api/v1/boarding-houses?q=Xuan+Thuy");
        var kwPaged = await kwResp.ReadAsync<PagedResponse<PublicBoardingHouseCardResponse>>();
        Assert.Contains(kwPaged.Items, h => h.Id == house1.Id);

        var kwMissResp = await _client.GetAsync("/api/v1/boarding-houses?q=NonExistentKeyword");
        var kwMissPaged = await kwMissResp.ReadAsync<PagedResponse<PublicBoardingHouseCardResponse>>();
        Assert.DoesNotContain(kwMissPaged.Items, h => h.Id == house1.Id);

        // 3. Price filter
        var priceResp = await _client.GetAsync("/api/v1/boarding-houses?minPrice=4000000&maxPrice=5000000");
        Assert.Equal(HttpStatusCode.OK, priceResp.StatusCode);
        var pricePaged = await priceResp.ReadAsync<PagedResponse<PublicBoardingHouseCardResponse>>();
        Assert.Contains(pricePaged.Items, h => h.Id == house1.Id);

        var priceMissResp = await _client.GetAsync("/api/v1/boarding-houses?minPrice=6000000");
        var priceMissPaged = await priceMissResp.ReadAsync<PagedResponse<PublicBoardingHouseCardResponse>>();
        Assert.DoesNotContain(priceMissPaged.Items, h => h.Id == house1.Id);
    }

    [Fact]
    public async Task Nearby_spatial_search_uses_postgis_and_orders_by_distance()
    {
        var ownerToken = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        // Anchor 1: HUST (Hai Ba Trung, HN) - 21.0045, 105.8435
        var houseHustResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                "Trọ Bách Khoa HUST",
                null,
                BoardingHouseType.Traditional,
                "1 Dai Co Viet",
                "Bach Khoa",
                "Hai Ba Trung",
                "Ha Noi",
                21.0045m,
                105.8435m));
        houseHustResp.EnsureSuccessStatusCode();
        var houseHust = await houseHustResp.ReadAsync<BoardingHouseDetailResponse>();

        // Anchor 2: VNU Xuan Thuy (Cau Giay, HN) - 21.0378, 105.7825 (approx 7.5 km away)
        var houseVnuResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                "Trọ Xuân Thủy VNU",
                null,
                BoardingHouseType.MiniHouse,
                "144 Xuan Thuy",
                "Dich Vong Hau",
                "Cau Giay",
                "Ha Noi",
                21.0378m,
                105.7825m));
        houseVnuResp.EnsureSuccessStatusCode();
        var houseVnu = await houseVnuResp.ReadAsync<BoardingHouseDetailResponse>();

        // Publish both
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var h1 = await db.BoardingHouses.FirstAsync(x => x.Id == houseHust.Id);
            h1.ListingStatus = ListingStatus.Published;
            var h2 = await db.BoardingHouses.FirstAsync(x => x.Id == houseVnu.Id);
            h2.ListingStatus = ListingStatus.Published;
            await db.SaveChangesAsync();
        }

        // Search near HUST with radius 2km -> only HUST returned
        var nearby2kmResp = await _client.GetAsync(
            "/api/v1/boarding-houses/nearby?lat=21.0045&lon=105.8435&radiusKm=2");
        Assert.Equal(HttpStatusCode.OK, nearby2kmResp.StatusCode);
        var nearby2km = await nearby2kmResp.ReadAsync<IReadOnlyList<BoardingHouseNearbyResponse>>();
        Assert.Contains(nearby2km, h => h.Id == houseHust.Id);
        Assert.DoesNotContain(nearby2km, h => h.Id == houseVnu.Id);

        // Search near HUST with radius 15km -> both returned, HUST first (closer)
        var nearby15kmResp = await _client.GetAsync(
            "/api/v1/boarding-houses/nearby?lat=21.0045&lon=105.8435&radiusKm=15");
        Assert.Equal(HttpStatusCode.OK, nearby15kmResp.StatusCode);
        var nearby15km = await nearby15kmResp.ReadAsync<IReadOnlyList<BoardingHouseNearbyResponse>>();
        Assert.Contains(nearby15km, h => h.Id == houseHust.Id);
        Assert.Contains(nearby15km, h => h.Id == houseVnu.Id);

        var hustResult = nearby15km.First(h => h.Id == houseHust.Id);
        var vnuResult = nearby15km.First(h => h.Id == houseVnu.Id);
        Assert.True(hustResult.DistanceMeters < vnuResult.DistanceMeters);
        Assert.True(hustResult.DistanceMeters < 1000); // Nearby within 1km
        Assert.True(vnuResult.DistanceMeters > 5000); // 7.6km away
    }

    [Fact]
    public async Task Map_and_detail_and_vacant_rooms_endpoints_work()
    {
        var ownerToken = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        var houseResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                "Nha Tro Cau Giay Map Test",
                "Full detail test description",
                BoardingHouseType.MiniHouse,
                "100 Cau Giay",
                "Quan Hoa",
                "Cau Giay",
                "Ha Noi",
                21.0300m,
                105.7900m));
        houseResp.EnsureSuccessStatusCode();
        var house = await houseResp.ReadAsync<BoardingHouseDetailResponse>();

        var rtResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{house.Id}/room-types",
            ownerToken,
            new SaveRoomTypeRequest("Standard Room", 3000000, 20, 1, "Room desc", []));
        rtResp.EnsureSuccessStatusCode();
        var rt = await rtResp.ReadAsync<RoomTypeResponse>();

        var roomResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{house.Id}/rooms",
            ownerToken,
            new SaveRoomRequest(rt.Id, "201", "Available room"));
        roomResp.EnsureSuccessStatusCode();

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var h = await db.BoardingHouses.FirstAsync(x => x.Id == house.Id);
            h.ListingStatus = ListingStatus.Published;
            await db.SaveChangesAsync();
        }

        // 1. Map bounding box
        var mapResp = await _client.GetAsync(
            "/api/v1/boarding-houses/map?swLat=21.0&swLon=105.7&neLat=21.1&neLon=105.9");
        Assert.Equal(HttpStatusCode.OK, mapResp.StatusCode);
        var markers = await mapResp.ReadAsync<IReadOnlyList<BoardingHouseMapMarkerResponse>>();
        Assert.Contains(markers, m => m.Id == house.Id);

        // 2. Detail
        var detailResp = await _client.GetAsync($"/api/v1/boarding-houses/{house.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);
        var detail = await detailResp.ReadAsync<PublicBoardingHouseDetailResponse>();
        Assert.Equal("Nha Tro Cau Giay Map Test", detail.Name);
        Assert.Equal("Full detail test description", detail.Description);
        Assert.Equal(1, detail.TotalRoomsCount);
        Assert.Equal(1, detail.AvailableRoomsCount);

        // 3. Vacant rooms
        var roomsResp = await _client.GetAsync($"/api/v1/boarding-houses/{house.Id}/rooms");
        Assert.Equal(HttpStatusCode.OK, roomsResp.StatusCode);
        var rooms = await roomsResp.ReadAsync<IReadOnlyList<PublicVacantRoomResponse>>();
        Assert.Single(rooms);
        Assert.Equal("201", rooms[0].RoomNumber);

        // 4. Facilities list
        var facResp = await _client.GetAsync("/api/v1/facilities");
        Assert.Equal(HttpStatusCode.OK, facResp.StatusCode);

        // 5. Provinces list
        var provResp = await _client.GetAsync("/api/v1/provinces");
        Assert.Equal(HttpStatusCode.OK, provResp.StatusCode);

        // 6. Districts list
        var distResp = await _client.GetAsync("/api/v1/provinces/HN/districts");
        Assert.Equal(HttpStatusCode.OK, distResp.StatusCode);
    }
}
