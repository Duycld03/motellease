using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Reports.Contracts;
using MotelLease.Application.Reviews.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class ReviewsAndReportsFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public ReviewsAndReportsFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Invariant_9_10_verified_review_and_rating_recomputation()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var ownerId = await _client.UserIdAsync(listing.OwnerToken);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await _client.UserIdAsync(tenantToken);

        var randomTenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        // 1. User without lease cannot review (Invariant §9.10)
        var unverifiedResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/reviews",
            randomTenantToken,
            new CreateReviewRequest(listing.HouseId, 5, "I never stayed here but looks cool"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unverifiedResp.StatusCode);

        // 2. Create an active lease for tenant
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
                FullName = "Tenant One",
                IsPrimary = true,
                MovedInAt = DateTimeOffset.UtcNow.AddMonths(-1)
            });
            db.Leases.Add(lease);
            await db.SaveChangesAsync();
        }

        // 3. Tenant with lease posts verified review
        var reviewResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/reviews",
            tenantToken,
            new CreateReviewRequest(listing.HouseId, 4, "Phòng đẹp, chủ nhà nhiệt tình"));
        Assert.Equal(HttpStatusCode.Created, reviewResp.StatusCode);
        var review = await reviewResp.ReadAsync<ReviewResponse>();
        Assert.True(review.IsVerified);
        Assert.NotNull(review.LeaseId);
        Assert.Equal((short)4, review.Rating);

        // Verify boarding house rating updated
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var house = await db.BoardingHouses.FirstAsync(b => b.Id == listing.HouseId);
            Assert.Equal(4.0m, house.Rating);
            Assert.Equal(1, house.ReviewCount);
        }

        // 4. Invariant §9.10: One review per (UserId, LeaseId) - Duplicate rejected
        var dupResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/reviews",
            tenantToken,
            new CreateReviewRequest(listing.HouseId, 5, "Another review on same lease"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dupResp.StatusCode);

        // 5. Tenant updates review rating to 5
        var updateResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/reviews/{review.Id}",
            tenantToken,
            new UpdateReviewRequest(5, "Đã sửa thành 5 sao tuyệt vời"));
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var house = await db.BoardingHouses.FirstAsync(b => b.Id == listing.HouseId);
            Assert.Equal(5.0m, house.Rating);
            Assert.Equal(1, house.ReviewCount);
        }

        // 6. Tenant lists own reviews
        var myReviewsResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/me/reviews",
            tenantToken);
        Assert.Equal(HttpStatusCode.OK, myReviewsResp.StatusCode);
        var myReviews = await myReviewsResp.ReadAsync<PagedResponse<ReviewResponse>>();
        Assert.Single(myReviews.Items);
        Assert.Equal(review.Id, myReviews.Items[0].Id);

        // 7. Tenant deletes review -> rating & count reset to 0
        var deleteResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/reviews/{review.Id}",
            tenantToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var house = await db.BoardingHouses.FirstAsync(b => b.Id == listing.HouseId);
            Assert.Equal(0.0m, house.Rating);
            Assert.Equal(0, house.ReviewCount);
        }
    }

    [Fact]
    public async Task Owner_can_reply_to_tenant_review()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var ownerId = await _client.UserIdAsync(listing.OwnerToken);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await _client.UserIdAsync(tenantToken);

        // Create lease for tenant
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
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
                FullName = "Tenant One",
                IsPrimary = true,
                MovedInAt = DateTimeOffset.UtcNow.AddMonths(-1)
            });
            db.Leases.Add(lease);
            await db.SaveChangesAsync();
        }

        // Post review
        var reviewResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/reviews",
            tenantToken,
            new CreateReviewRequest(listing.HouseId, 5, "Nhà trọ rất sạch sẽ"));
        var review = await reviewResp.ReadAsync<ReviewResponse>();

        // 1. Owner replies to review
        var replyResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/reviews/{review.Id}/reply",
            listing.OwnerToken,
            new ReplyReviewRequest("Cảm ơn bạn đã ủng hộ!"));
        Assert.Equal(HttpStatusCode.OK, replyResp.StatusCode);
        var reply = await replyResp.ReadAsync<ReviewReplyResponse>();
        Assert.Equal("Cảm ơn bạn đã ủng hộ!", reply.Content);

        // 2. Owner lists property reviews
        var propertyReviewsResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/reviews?boardingHouseId={listing.HouseId}",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, propertyReviewsResp.StatusCode);
        var propReviews = await propertyReviewsResp.ReadAsync<PagedResponse<ReviewResponse>>();
        Assert.Single(propReviews.Items);
        Assert.Single(propReviews.Items[0].Replies);
        Assert.Equal(reply.Id, propReviews.Items[0].Replies[0].Id);

        // 3. Update reply
        var updateReplyResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/reviews/{review.Id}/reply/{reply.Id}",
            listing.OwnerToken,
            new ReplyReviewRequest("Cảm ơn bạn nhiều nhé!"));
        Assert.Equal(HttpStatusCode.OK, updateReplyResp.StatusCode);

        // 4. Delete reply
        var deleteReplyResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/reviews/{review.Id}/reply/{reply.Id}",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteReplyResp.StatusCode);
    }

    [Fact]
    public async Task Reports_lifecycle_create_list_and_moderate()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        using (var scope = _app.Services.CreateScope())
        {
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
        }
        var adminToken = await _client.LoginAsync(adminEmail);

        // 1. Tenant files report on boarding house
        var reportResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/reports",
            tenantToken,
            new CreateReportRequest(
                ReportTargetType.BoardingHouse,
                listing.HouseId,
                "Địa chỉ không chính xác",
                "Số nhà trên bài đăng sai lệch so với thực tế."));
        Assert.Equal(HttpStatusCode.Created, reportResp.StatusCode);
        var report = await reportResp.ReadAsync<ReportResponse>();
        Assert.Equal(ReportStatus.Pending, report.Status);

        // 2. Tenant views own reports
        var myReportsResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/me/reports",
            tenantToken);
        Assert.Equal(HttpStatusCode.OK, myReportsResp.StatusCode);
        var myReports = await myReportsResp.ReadAsync<PagedResponse<ReportResponse>>();
        Assert.Contains(myReports.Items, r => r.Id == report.Id);

        // 3. Admin lists reports
        var adminListResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/reports?status=Pending",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, adminListResp.StatusCode);
        var adminList = await adminListResp.ReadAsync<PagedResponse<ReportResponse>>();
        Assert.Contains(adminList.Items, r => r.Id == report.Id);

        // 4. Admin gets report detail
        var adminGetResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/reports/{report.Id}",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, adminGetResp.StatusCode);

        // 5. Admin resolves report
        var resolveResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/reports/{report.Id}/resolve",
            adminToken,
            new ResolveReportRequest("Đã nhắc nhở chủ nhà cập nhật địa chỉ chính xác."));
        Assert.Equal(HttpStatusCode.OK, resolveResp.StatusCode);
        var resolved = await resolveResp.ReadAsync<ReportResponse>();
        Assert.Equal(ReportStatus.Resolved, resolved.Status);
        Assert.Equal("Đã nhắc nhở chủ nhà cập nhật địa chỉ chính xác.", resolved.Resolution);

        // 6. Resolving already processed report is rejected
        var dupResolveResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/reports/{report.Id}/resolve",
            adminToken,
            new ResolveReportRequest("Trùng lặp"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dupResolveResp.StatusCode);
    }
}
