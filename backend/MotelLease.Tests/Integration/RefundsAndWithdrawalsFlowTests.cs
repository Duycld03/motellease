using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Deposits.Contracts;
using MotelLease.Application.Refunds.Contracts;
using MotelLease.Application.Withdrawals.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class RefundsAndWithdrawalsFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public RefundsAndWithdrawalsFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Deposit_refund_lifecycle_approve_and_reject()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await _client.UserIdAsync(tenantToken);

        // 1. Create a paid deposit
        Guid depositId;
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var deposit = new Deposit
            {
                RoomId = listing.RoomId,
                UserId = tenantId,
                Amount = 1_500_000,
                RequestedStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                RequestedTermMonths = 6,
                Status = DepositStatus.Paid
            };
            db.Deposits.Add(deposit);
            await db.SaveChangesAsync();
            depositId = deposit.Id;
        }

        // 2. Tenant requests deposit refund
        var refundResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/refund-requests",
            tenantToken,
            new CreateRefundRequest(depositId, "Thay đổi kế hoạch chuyển nhà"));
        Assert.Equal(HttpStatusCode.Created, refundResp.StatusCode);
        var refund = await refundResp.ReadAsync<RefundRequestResponse>();
        Assert.Equal(RequestStatus.Pending, refund.Status);
        Assert.Equal(1_500_000m, refund.Amount);

        // Verify deposit status is now Refunding
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var dep = await db.Deposits.FirstAsync(d => d.Id == depositId);
            Assert.Equal(DepositStatus.Refunding, dep.Status);
        }

        // 3. Duplicate refund request is rejected (422 UnprocessableEntity)
        var dupRefundResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/refund-requests",
            tenantToken,
            new CreateRefundRequest(depositId, "Gửi lại"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dupRefundResp.StatusCode);

        // 4. Owner approves deposit refund
        var approveResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/refund-requests/{refund.Id}/approve",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approved = await approveResp.ReadAsync<RefundRequestResponse>();
        Assert.Equal(RequestStatus.Accepted, approved.Status);

        // Verify deposit status is Refunded & notification was sent
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var dep = await db.Deposits.FirstAsync(d => d.Id == depositId);
            Assert.Equal(DepositStatus.Refunded, dep.Status);

            var notif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == tenantId && n.Type == NotificationType.RefundProcessed);
            Assert.NotNull(notif);
        }

        // 5. Test Reject flow with another paid deposit
        Guid secondDepositId;
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var deposit2 = new Deposit
            {
                RoomId = listing.RoomId,
                UserId = tenantId,
                Amount = 2_000_000,
                RequestedStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                RequestedTermMonths = 6,
                Status = DepositStatus.Paid
            };
            db.Deposits.Add(deposit2);
            await db.SaveChangesAsync();
            secondDepositId = deposit2.Id;
        }

        var refundResp2 = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/refund-requests",
            tenantToken,
            new CreateRefundRequest(secondDepositId, "Lý do khác"));
        var refund2 = await refundResp2.ReadAsync<RefundRequestResponse>();

        var rejectResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/refund-requests/{refund2.Id}/reject",
            listing.OwnerToken,
            new RejectRefundRequest("Đã quá thời hạn hủy cọc miễn phí"));
        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejected = await rejectResp.ReadAsync<RefundRequestResponse>();
        Assert.Equal(RequestStatus.Rejected, rejected.Status);
        Assert.Equal("Đã quá thời hạn hủy cọc miễn phí", rejected.RejectReason);

        // Verify deposit reverted to Paid
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var dep = await db.Deposits.FirstAsync(d => d.Id == secondDepositId);
            Assert.Equal(DepositStatus.Paid, dep.Status);
        }
    }

    [Fact]
    public async Task Invariant_9_11_owner_withdrawal_balance_guard_and_lifecycle()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var ownerId = await _client.UserIdAsync(listing.OwnerToken);
        var adminToken = await SeedAdminAsync();

        // Seed available balance for owner
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var profile = await db.OwnerProfiles.FirstAsync(op => op.UserId == ownerId);
            profile.AvailableBalance = 5_000_000m;
            profile.BankName = "Vietcombank";
            profile.BankAccountNumber = "1234567890";
            profile.BankAccountHolder = "NGUYEN VAN OWNER";
            await db.SaveChangesAsync();
        }

        // 1. Invariant §9.11: Request exceeding AvailableBalance is rejected (422 UnprocessableEntity)
        var invalidWithdrawResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/withdraw-requests",
            listing.OwnerToken,
            new CreateWithdrawRequest(6_000_000m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidWithdrawResp.StatusCode);

        // 2. Valid withdrawal request for 2,000,000 VND
        var withdrawResp1 = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/withdraw-requests",
            listing.OwnerToken,
            new CreateWithdrawRequest(2_000_000m));
        Assert.Equal(HttpStatusCode.Created, withdrawResp1.StatusCode);
        var withdraw1 = await withdrawResp1.ReadAsync<WithdrawRequestResponse>();
        Assert.Equal(RequestStatus.Pending, withdraw1.Status);
        Assert.Equal(2_000_000m, withdraw1.Amount);
        Assert.Equal("Vietcombank", withdraw1.BankName);

        // Verify balance is deducted immediately (5,000,000 - 2,000,000 = 3,000,000)
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var profile = await db.OwnerProfiles.FirstAsync(op => op.UserId == ownerId);
            Assert.Equal(3_000_000m, profile.AvailableBalance);
        }

        // 3. Admin approves first withdrawal
        var approveResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/withdraw-requests/{withdraw1.Id}/approve",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approved = await approveResp.ReadAsync<WithdrawRequestResponse>();
        Assert.Equal(RequestStatus.Accepted, approved.Status);

        // Verify notification sent to owner
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var notif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == ownerId && n.Type == NotificationType.WithdrawHandled);
            Assert.NotNull(notif);
        }

        // 4. Owner creates second withdrawal request for 1,000,000 VND
        var withdrawResp2 = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/withdraw-requests",
            listing.OwnerToken,
            new CreateWithdrawRequest(1_000_000m));
        Assert.Equal(HttpStatusCode.Created, withdrawResp2.StatusCode);
        var withdraw2 = await withdrawResp2.ReadAsync<WithdrawRequestResponse>();

        // Verify balance is deducted to 2,000,000
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var profile = await db.OwnerProfiles.FirstAsync(op => op.UserId == ownerId);
            Assert.Equal(2_000_000m, profile.AvailableBalance);
        }

        // 5. Admin rejects second withdrawal -> balance restored back to 3,000,000 VND
        var rejectResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/withdraw-requests/{withdraw2.Id}/reject",
            adminToken,
            new RejectWithdrawRequest("Thông tin số tài khoản chưa khớp với tên người thụ hưởng"));
        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejected = await rejectResp.ReadAsync<WithdrawRequestResponse>();
        Assert.Equal(RequestStatus.Rejected, rejected.Status);

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var profile = await db.OwnerProfiles.FirstAsync(op => op.UserId == ownerId);
            Assert.Equal(3_000_000m, profile.AvailableBalance);
        }
    }
}
