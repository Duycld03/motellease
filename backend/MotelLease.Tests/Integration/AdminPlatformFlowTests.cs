using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Admin.Contracts;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Catalogue.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Reviews.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class AdminPlatformFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public AdminPlatformFlowTests(PostgresFixture postgres) => _postgres = postgres;

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

    private async Task<string> SeedAdminAsync()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
        var adminUser = new User
        {
            Email = adminEmail,
            Username = $"admin{Guid.NewGuid():N}"[..16],
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ApiRequests.Password, 4),
            FullName = "System Admin",
            Role = UserRole.Admin,
            EmailConfirmed = true,
            IsLocked = false
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        return await _client.LoginAsync(adminEmail);
    }

    [Fact]
    public async Task Admin_accounts_management_flow_and_guards()
    {
        var adminToken = await SeedAdminAsync();
        var adminId = await _client.UserIdAsync(adminToken);

        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        // 1. Non-admin forbidden
        var nonAdminResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/admin/accounts",
            tenantToken);
        Assert.Equal(HttpStatusCode.Forbidden, nonAdminResp.StatusCode);

        // 2. Admin creates account
        var newEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var newUsername = $"user{Guid.NewGuid():N}"[..16];
        var createResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/admin/accounts",
            adminToken,
            new AdminCreateAccountRequest(
                Email: newEmail,
                Username: newUsername,
                Password: ApiRequests.Password,
                FullName: "Nguyễn Văn Owner",
                PhoneNumber: "0912345678",
                Gender: Gender.Male,
                Role: UserRole.Owner));

        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.ReadAsync<AdminAccountSummaryResponse>();
        Assert.Equal(newEmail, created.Email);
        Assert.Equal(UserRole.Owner, created.Role);

        // 3. Duplicate email rejected
        var dupResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/admin/accounts",
            adminToken,
            new AdminCreateAccountRequest(
                Email: newEmail,
                Username: $"other{Guid.NewGuid():N}"[..16],
                Password: ApiRequests.Password,
                FullName: "Duplicate Email",
                PhoneNumber: null,
                Gender: Gender.Other,
                Role: UserRole.Tenant));
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

        // 4. List accounts with search filter
        var listResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/admin/accounts?search={newUsername}",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var list = await listResp.ReadAsync<PagedResponse<AdminAccountSummaryResponse>>();
        Assert.Single(list.Items);
        Assert.Equal(created.Id, list.Items[0].Id);

        // 5. Get account detail
        var detailResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/admin/accounts/{created.Id}",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);
        var detail = await detailResp.ReadAsync<AdminAccountDetailResponse>();
        Assert.Equal(created.Id, detail.Id);
        Assert.Equal(0, detail.BoardingHousesCount);

        // 6. Update account
        var updateResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/admin/accounts/{created.Id}",
            adminToken,
            new AdminUpdateAccountRequest(
                FullName: "Nguyễn Văn Owner Updated",
                PhoneNumber: "0987654321",
                Gender: Gender.Male,
                Role: UserRole.Owner));
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.ReadAsync<AdminAccountSummaryResponse>();
        Assert.Equal("Nguyễn Văn Owner Updated", updated.FullName);

        // 7. Lock account
        var lockResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/admin/accounts/{created.Id}/lock",
            adminToken,
            new AdminLockAccountRequest("Vi phạm chính sách cộng đồng"));
        Assert.Equal(HttpStatusCode.NoContent, lockResp.StatusCode);

        // Created user cannot log in while locked
        var loginResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/auth/login",
            string.Empty,
            new LoginRequest(newEmail, ApiRequests.Password));
        Assert.Equal(HttpStatusCode.Forbidden, loginResp.StatusCode);

        // 8. Unlock account
        var unlockResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/admin/accounts/{created.Id}/unlock",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, unlockResp.StatusCode);

        // Can log in now
        var loginAfterUnlock = await _client.LoginAsync(newEmail);
        Assert.NotNull(loginAfterUnlock);

        // 9. Admin cannot lock or delete self
        var lockSelf = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/admin/accounts/{adminId}/lock",
            adminToken,
            new AdminLockAccountRequest("Lock myself"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, lockSelf.StatusCode);

        var deleteSelf = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/admin/accounts/{adminId}",
            adminToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, deleteSelf.StatusCode);

        // 10. Delete and restore account
        var deleteResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/admin/accounts/{created.Id}",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var restoreResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/admin/accounts/{created.Id}/restore",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, restoreResp.StatusCode);
    }

    [Fact]
    public async Task Admin_boarding_houses_moderation_and_facilities_crud()
    {
        var adminToken = await SeedAdminAsync();
        var ownerToken = await _client.RegisterAsync(_app.Emails, UserRole.Owner);

        // 1. Owner creates a draft house
        var createHouseResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/boarding-houses",
            ownerToken,
            new SaveBoardingHouseRequest(
                Name: "Nhà trọ Cầu Giấy Admin Test",
                Description: "Mô tả nhà trọ",
                Type: BoardingHouseType.DormStyle,
                AddressLine: "123 Cầu Giấy",
                Ward: "Dịch Vọng",
                District: "Cầu Giấy",
                Province: "Hà Nội",
                Latitude: 21.0333m,
                Longitude: 105.7833m));
        var house = await createHouseResp.ReadAsync<BoardingHouseDetailResponse>();

        // 2. Admin lists houses
        var listHousesResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/admin/boarding-houses?listingStatus=Draft",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, listHousesResp.StatusCode);
        var listHouses = await listHousesResp.ReadAsync<PagedResponse<AdminBoardingHouseResponse>>();
        Assert.Contains(listHouses.Items, h => h.Id == house.Id);

        // 3. Admin rejects house
        var rejectResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/admin/boarding-houses/{house.Id}/reject",
            adminToken,
            new AdminRejectListingRequest("Thiếu giấy phép kinh doanh"));
        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejected = await rejectResp.ReadAsync<AdminBoardingHouseResponse>();
        Assert.Equal(ListingStatus.Rejected, rejected.ListingStatus);
        Assert.Equal("Thiếu giấy phép kinh doanh", rejected.RejectionReason);

        // 4. Admin approves house
        var approveResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/admin/boarding-houses/{house.Id}/approve",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approved = await approveResp.ReadAsync<AdminBoardingHouseResponse>();
        Assert.Equal(ListingStatus.Published, approved.ListingStatus);
        Assert.Null(approved.RejectionReason);

        // 5. Admin soft deletes and restores house
        var deleteHouseResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/admin/boarding-houses/{house.Id}",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteHouseResp.StatusCode);

        var restoreHouseResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/admin/boarding-houses/{house.Id}/restore",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, restoreHouseResp.StatusCode);

        // 6. Admin Facilities CRUD
        var facName = $"Tiện ích {Guid.NewGuid():N}"[..20];
        var createFacResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/admin/facilities",
            adminToken,
            new CreateFacilityRequest(
                Name: facName,
                CodeName: $"code_{Guid.NewGuid():N}"[..16],
                IconKey: "icon_wifi",
                Description: "Mạng internet tốc độ cao"));
        Assert.Equal(HttpStatusCode.Created, createFacResp.StatusCode);
        var facility = await createFacResp.ReadAsync<FacilityDetailResponse>();
        Assert.Equal(facName, facility.Name);

        // Get Facility
        var getFacResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/admin/facilities/{facility.Id}",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, getFacResp.StatusCode);

        // Update Facility
        var updateFacResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/admin/facilities/{facility.Id}",
            adminToken,
            new UpdateFacilityRequest(
                Name: facName + " Updated",
                CodeName: facility.CodeName,
                IconKey: "icon_wifi_fast",
                Description: "Internet cáp quang 1Gbps"));
        Assert.Equal(HttpStatusCode.OK, updateFacResp.StatusCode);

        // Delete Facility
        var deleteFacResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/admin/facilities/{facility.Id}",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteFacResp.StatusCode);
    }

    [Fact]
    public async Task Admin_reviews_moderation_audit_logs_and_stats_summary()
    {
        var adminToken = await SeedAdminAsync();
        var listing = await _app.PublishedListingAsync(_client);
        var ownerId = await _client.UserIdAsync(listing.OwnerToken);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await _client.UserIdAsync(tenantToken);

        // Create an active lease for tenant so Invariant §9.10 is satisfied
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var room = await db.Rooms.FirstAsync(r => r.Id == listing.RoomId);
            room.Status = RoomStatus.Occupied;

            var lease = new Lease
            {
                RoomId = listing.RoomId,
                PrimaryTenantUserId = tenantId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(5)),
                TermMonths = 6,
                MonthlyRent = 3_000_000,
                DepositHeld = 3_000_000,
                Status = LeaseStatus.Active,
                CreatedByUserId = ownerId
            };
            lease.Tenants.Add(new LeaseTenant
            {
                UserId = tenantId,
                FullName = "Tenant Reviewer",
                IsPrimary = true,
                MovedInAt = DateTimeOffset.UtcNow.AddMonths(-1)
            });
            db.Leases.Add(lease);
            await db.SaveChangesAsync();
        }

        // Tenant posts a review
        var postReviewResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/reviews",
            tenantToken,
            new CreateReviewRequest(
                BoardingHouseId: listing.HouseId,
                Rating: 5,
                Content: "Phòng trọ rất sạch sẽ và an ninh tốt!"));
        Assert.Equal(HttpStatusCode.Created, postReviewResp.StatusCode);
        var review = await postReviewResp.ReadAsync<ReviewResponse>();

        // 1. Admin lists reviews
        var listRevResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/admin/reviews?boardingHouseId={listing.HouseId}",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, listRevResp.StatusCode);
        var listRev = await listRevResp.ReadAsync<PagedResponse<AdminReviewResponse>>();
        Assert.Contains(listRev.Items, r => r.Id == review.Id);

        // 2. Admin soft-deletes review -> house rating recomputed
        var delRevResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/admin/reviews/{review.Id}",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, delRevResp.StatusCode);

        var publicHouseAfterDelete = await _client.GetAsync($"/api/v1/boarding-houses/{listing.HouseId}");
        var houseAfterDelete = await publicHouseAfterDelete.ReadAsync<PublicBoardingHouseDetailResponse>();
        Assert.Equal(0, houseAfterDelete.ReviewCount);

        // 3. Admin restores review -> house rating recomputed
        var restoreRevResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/admin/reviews/{review.Id}/restore",
            adminToken);
        Assert.Equal(HttpStatusCode.NoContent, restoreRevResp.StatusCode);

        var publicHouseAfterRestore = await _client.GetAsync($"/api/v1/boarding-houses/{listing.HouseId}");
        var houseAfterRestore = await publicHouseAfterRestore.ReadAsync<PublicBoardingHouseDetailResponse>();
        Assert.Equal(1, houseAfterRestore.ReviewCount);
        Assert.Equal(5.0m, houseAfterRestore.Rating);

        // 4. Admin queries Audit Logs
        var auditLogsResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/admin/audit-logs",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, auditLogsResp.StatusCode);
        var auditLogs = await auditLogsResp.ReadAsync<PagedResponse<AuditLogResponse>>();
        Assert.NotEmpty(auditLogs.Items);

        // 5. Admin queries Platform Stats Summary
        var statsResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/admin/stats/summary",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, statsResp.StatusCode);
        var stats = await statsResp.ReadAsync<AdminPlatformStatsResponse>();
        Assert.True(stats.TotalUsers > 0);
        Assert.True(stats.TotalBoardingHouses > 0);
        Assert.True(stats.TotalRooms > 0);
    }
}
