using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Maintenance.Contracts;
using MotelLease.Application.Staff.Contracts;
using MotelLease.Application.Tasks.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class StaffTasksAndMaintenanceFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public StaffTasksAndMaintenanceFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Staff_management_and_resource_authorization_lifecycle()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var otherListing = await _app.PublishedListingAsync(_client);

        var staffEmail = $"staff-{Guid.NewGuid():N}@example.com";
        var staffUsername = $"staff{Guid.NewGuid():N}"[..16];

        // 1. Owner creates a staff account
        var createStaffResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/staff",
            listing.OwnerToken,
            new CreateStaffRequest(
                staffUsername,
                staffEmail,
                ApiRequests.Password,
                "Staff Member One",
                "0987654321",
                Gender.Female,
                DateOnly.FromDateTime(DateTime.UtcNow)));
        Assert.Equal(HttpStatusCode.Created, createStaffResp.StatusCode);
        var staffDetail = await createStaffResp.ReadAsync<StaffDetailResponse>();
        Assert.Equal(staffUsername, staffDetail.Username);
        Assert.Equal(staffEmail, staffDetail.Email);

        // 2. Staff logs in
        var loginResp = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(staffEmail, ApiRequests.Password));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var staffTokens = await loginResp.ReadAsync<AuthTokensResponse>();
        var staffToken = staffTokens.AccessToken;

        // 3. Staff tries to access rooms of listing before assignment -> 403 Forbidden
        var unassignedResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/rooms",
            staffToken);
        Assert.Equal(HttpStatusCode.Forbidden, unassignedResp.StatusCode);

        // 4. Owner assigns staff to boarding house
        var assignResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/staff",
            listing.OwnerToken,
            new AssignStaffRequest(staffDetail.Id));
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        // Duplicate assignment -> 409 Conflict
        var dupAssignResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/staff",
            listing.OwnerToken,
            new AssignStaffRequest(staffDetail.Id));
        Assert.Equal(HttpStatusCode.Conflict, dupAssignResp.StatusCode);

        // 5. Assigned staff can now view rooms of listing
        var assignedResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/rooms",
            staffToken);
        Assert.Equal(HttpStatusCode.OK, assignedResp.StatusCode);

        // But staff cannot access unassigned house -> 403 Forbidden
        var otherHouseResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{otherListing.HouseId}/rooms",
            staffToken);
        Assert.Equal(HttpStatusCode.Forbidden, otherHouseResp.StatusCode);

        // 6. Owner unassigns staff
        var unassignResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/staff/{staffDetail.Id}",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.NoContent, unassignResp.StatusCode);

        // Staff is no longer assigned -> 403 Forbidden
        var revokedResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/rooms",
            staffToken);
        Assert.Equal(HttpStatusCode.Forbidden, revokedResp.StatusCode);

        // 7. Owner locks staff account
        var lockResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/staff/{staffDetail.Id}",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.NoContent, lockResp.StatusCode);

        // Locked staff cannot log in
        var reloginResp = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(staffEmail, ApiRequests.Password));
        Assert.Equal(HttpStatusCode.Unauthorized, reloginResp.StatusCode);
    }

    [Fact]
    public async Task Work_tasks_lifecycle_assign_and_complete()
    {
        var listing = await _app.PublishedListingAsync(_client);

        var staffEmail = $"staff-{Guid.NewGuid():N}@example.com";
        var staffUsername = $"staff{Guid.NewGuid():N}"[..16];

        var createStaffResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/staff",
            listing.OwnerToken,
            new CreateStaffRequest(
                staffUsername,
                staffEmail,
                ApiRequests.Password,
                "Staff Member Two",
                "0987654322",
                Gender.Male,
                DateOnly.FromDateTime(DateTime.UtcNow)));
        var staffDetail = await createStaffResp.ReadAsync<StaffDetailResponse>();

        var staffToken = await _client.LoginAsync(staffEmail);

        // Assign staff to house
        await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/staff",
            listing.OwnerToken,
            new AssignStaffRequest(staffDetail.Id));

        // 1. Owner creates work task
        var createTaskResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/tasks",
            listing.OwnerToken,
            new CreateTaskRequest(
                listing.HouseId,
                staffDetail.Id,
                "Kiểm tra hệ thống PCCC",
                "Kiểm tra bình chữa cháy định kỳ tầng 1 và 2.",
                TaskPriority.High,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))));
        Assert.Equal(HttpStatusCode.Created, createTaskResp.StatusCode);
        var task = await createTaskResp.ReadAsync<TaskResponse>();
        Assert.Equal(WorkTaskStatus.InProgress, task.Status);
        Assert.Null(task.CompletedAt);

        // 2. Staff views assigned tasks
        var staffTasksResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/tasks?assignedTo={staffDetail.Id}",
            staffToken);
        Assert.Equal(HttpStatusCode.OK, staffTasksResp.StatusCode);
        var staffTasks = await staffTasksResp.ReadAsync<PagedResponse<TaskResponse>>();
        Assert.Single(staffTasks.Items);
        Assert.Equal(task.Id, staffTasks.Items[0].Id);

        // 3. Staff marks task as completed
        var updateStatusResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/tasks/{task.Id}/status",
            staffToken,
            new UpdateTaskStatusRequest(WorkTaskStatus.Completed));
        Assert.Equal(HttpStatusCode.OK, updateStatusResp.StatusCode);
        var updatedTask = await updateStatusResp.ReadAsync<TaskResponse>();
        Assert.Equal(WorkTaskStatus.Completed, updatedTask.Status);
        Assert.NotNull(updatedTask.CompletedAt);
    }

    [Fact]
    public async Task Maintenance_requests_flow_reported_accepted_and_resolved()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var ownerId = await _client.UserIdAsync(listing.OwnerToken);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await _client.UserIdAsync(tenantToken);

        // Create staff and assign to house
        var staffEmail = $"staff-{Guid.NewGuid():N}@example.com";
        var createStaffResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/staff",
            listing.OwnerToken,
            new CreateStaffRequest(
                $"staff{Guid.NewGuid():N}"[..16],
                staffEmail,
                ApiRequests.Password,
                "Staff Member Three",
                "0987654323",
                Gender.Male,
                DateOnly.FromDateTime(DateTime.UtcNow)));
        var staffDetail = await createStaffResp.ReadAsync<StaffDetailResponse>();
        var staffToken = await _client.LoginAsync(staffEmail);

        await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/staff",
            listing.OwnerToken,
            new AssignStaffRequest(staffDetail.Id));

        // Create active lease for tenant
        Guid leaseId;
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
            leaseId = lease.Id;
        }

        // 1. Tenant reports maintenance issue
        var reportResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/maintenance-requests",
            tenantToken,
            new CreateMaintenanceRequest(
                leaseId,
                MaintenanceCategory.Electricity,
                "Bóng đèn phòng ngủ bị chập chờn, cần thay bóng mới.",
                ["https://res.cloudinary.com/demo/image/upload/v1/sample.jpg"]));
        Assert.Equal(HttpStatusCode.Created, reportResp.StatusCode);
        var maintenance = await reportResp.ReadAsync<MaintenanceRequestResponse>();
        Assert.Equal(MaintenanceStatus.Open, maintenance.Status);
        Assert.Single(maintenance.Images);

        // 2. Notification dispatched to assigned staff
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var notif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == staffDetail.Id && n.Type == NotificationType.MaintenanceReported);
            Assert.NotNull(notif);
        }

        // 3. Assigned staff accepts maintenance request and assigns task to self
        var acceptResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/maintenance-requests/{maintenance.Id}/accept",
            staffToken,
            new AcceptMaintenanceRequest(
                AssignToStaffUserId: staffDetail.Id,
                TaskTitle: "Thay bóng đèn phòng 101",
                DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);
        var accepted = await acceptResp.ReadAsync<MaintenanceRequestResponse>();
        Assert.Equal(MaintenanceStatus.InProgress, accepted.Status);
        Assert.NotNull(accepted.TaskId);

        // Verify task was created
        var taskResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/tasks/{accepted.TaskId}",
            staffToken);
        Assert.Equal(HttpStatusCode.OK, taskResp.StatusCode);
        var task = await taskResp.ReadAsync<TaskResponse>();
        Assert.Equal(WorkTaskStatus.InProgress, task.Status);

        // 4. Staff resolves maintenance request -> auto completes task
        var resolveResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/maintenance-requests/{maintenance.Id}/resolve",
            staffToken);
        Assert.Equal(HttpStatusCode.OK, resolveResp.StatusCode);
        var resolved = await resolveResp.ReadAsync<MaintenanceRequestResponse>();
        Assert.Equal(MaintenanceStatus.Resolved, resolved.Status);

        // Check task is now completed
        var completedTaskResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/tasks/{accepted.TaskId}",
            staffToken);
        var completedTask = await completedTaskResp.ReadAsync<TaskResponse>();
        Assert.Equal(WorkTaskStatus.Completed, completedTask.Status);
        Assert.NotNull(completedTask.CompletedAt);
    }
}
