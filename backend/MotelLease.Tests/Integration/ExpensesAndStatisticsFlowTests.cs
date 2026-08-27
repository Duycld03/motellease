using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Expenses.Contracts;
using MotelLease.Application.Staff.Contracts;
using MotelLease.Application.Statistics.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class ExpensesAndStatisticsFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public ExpensesAndStatisticsFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Expenses_crud_flow_and_duplicate_guard()
    {
        var listing = await _app.PublishedListingAsync(_client);

        // Create a staff user and assign to house
        var staffEmail = $"staff-{Guid.NewGuid():N}@example.com";
        var createStaffResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/my/staff",
            listing.OwnerToken,
            new CreateStaffRequest(
                $"staff{Guid.NewGuid():N}"[..16],
                staffEmail,
                ApiRequests.Password,
                "Staff Expense Viewer",
                "0987654321",
                Gender.Male,
                DateOnly.FromDateTime(DateTime.UtcNow)));
        var staffDetail = await createStaffResp.ReadAsync<StaffDetailResponse>();
        var staffToken = await _client.LoginAsync(staffEmail);

        await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/staff",
            listing.OwnerToken,
            new AssignStaffRequest(staffDetail.Id));

        // 1. Owner creates monthly expense
        var otherExpenses = new List<OtherExpenseItem>
        {
            new("Internet", 220_000),
            new("Rác vệ sinh", 50_000)
        };

        var createResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses",
            listing.OwnerToken,
            new CreateExpenseRequest(
                Month: 5,
                Year: 2026,
                ElectricityOld: 100,
                ElectricityNew: 200,
                ElectricityQty: 100,
                ElectricityAmount: 350_000,
                WaterOld: 20,
                WaterNew: 30,
                WaterQty: 10,
                WaterAmount: 150_000,
                OtherExpenses: otherExpenses));

        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.ReadAsync<ExpenseResponse>();
        Assert.Equal(5, created.Month);
        Assert.Equal(2026, created.Year);
        Assert.Equal(270_000m, created.OtherExpensesTotal);
        Assert.Equal(770_000m, created.TotalExpense); // 350k + 150k + 270k

        // 2. Duplicate expense for same month and year is rejected (409 Conflict)
        var dupResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses",
            listing.OwnerToken,
            new CreateExpenseRequest(
                Month: 5,
                Year: 2026,
                ElectricityOld: 100,
                ElectricityNew: 200,
                ElectricityQty: 100,
                ElectricityAmount: 350_000,
                WaterOld: 20,
                WaterNew: 30,
                WaterQty: 10,
                WaterAmount: 150_000));
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

        // 3. Assigned staff can view expense
        var staffGetResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses/{created.Id}",
            staffToken);
        Assert.Equal(HttpStatusCode.OK, staffGetResp.StatusCode);
        var staffView = await staffGetResp.ReadAsync<ExpenseResponse>();
        Assert.Equal(created.Id, staffView.Id);

        // 4. Staff cannot delete expense (Owner only)
        var staffDeleteResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses/{created.Id}",
            staffToken);
        Assert.Equal(HttpStatusCode.Forbidden, staffDeleteResp.StatusCode);

        // 5. Owner updates expense
        var updatedResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses/{created.Id}",
            listing.OwnerToken,
            new UpdateExpenseRequest(
                ElectricityOld: 100,
                ElectricityNew: 250,
                ElectricityQty: 150,
                ElectricityAmount: 525_000,
                WaterOld: 20,
                WaterNew: 30,
                WaterQty: 10,
                WaterAmount: 150_000,
                OtherExpenses: [new OtherExpenseItem("Internet cáp quang", 300_000)]));
        Assert.Equal(HttpStatusCode.OK, updatedResp.StatusCode);
        var updated = await updatedResp.ReadAsync<ExpenseResponse>();
        Assert.Equal(525_000m, updated.ElectricityAmount);
        Assert.Equal(300_000m, updated.OtherExpensesTotal);
        Assert.Equal(975_000m, updated.TotalExpense); // 525k + 150k + 300k

        // 6. Owner lists expenses
        var listResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses?year=2026",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var list = await listResp.ReadAsync<PagedResponse<ExpenseResponse>>();
        Assert.Single(list.Items);

        // 7. Owner deletes expense
        var deleteResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses/{created.Id}",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getAfterDelete = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/my/boarding-houses/{listing.HouseId}/expenses/{created.Id}",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Owner_statistics_revenue_occupancy_profit_and_summary()
    {
        var listing = await _app.PublishedListingAsync(_client);
        var ownerId = await _client.UserIdAsync(listing.OwnerToken);
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await _client.UserIdAsync(tenantToken);

        // Create second room in the same house
        Guid secondRoomId;
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var room2 = new Room
            {
                BoardingHouseId = listing.HouseId,
                RoomTypeId = listing.RoomTypeId,
                RoomNumber = "102",
                Status = RoomStatus.Available
            };
            db.Rooms.Add(room2);

            // Update Room 1 to Occupied
            var room1 = await db.Rooms.FirstAsync(r => r.Id == listing.RoomId);
            room1.Status = RoomStatus.Occupied;

            // Create active lease for Room 1
            var lease = new Lease
            {
                RoomId = listing.RoomId,
                PrimaryTenantUserId = tenantId,
                DepositHeld = 3_000_000,
                MonthlyRent = 3_000_000,
                TermMonths = 6,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(4)),
                CreatedByUserId = ownerId,
                Status = LeaseStatus.Active
            };
            db.Leases.Add(lease);
            await db.SaveChangesAsync();

            // Create a Paid bill for Month 5, 2026
            var paidBill = new PaymentBill
            {
                LeaseId = lease.Id,
                RoomId = listing.RoomId,
                Month = 5,
                Year = 2026,
                RentAmount = 3_000_000,
                ElectricityOld = 100,
                ElectricityNew = 200,
                ElectricityQty = 100,
                ElectricityUnitPrice = 3_500,
                ElectricityAmount = 350_000,
                WaterOld = 10,
                WaterNew = 20,
                WaterQty = 10,
                WaterUnitPrice = 15_000,
                WaterAmount = 150_000,
                AdditionalFeeTotal = 0,
                TotalAmount = 3_500_000,
                Status = BillStatus.Paid
            };
            db.PaymentBills.Add(paidBill);

            // Create monthly expense for Month 5, 2026
            var expense = new BoardingHouseExpense
            {
                BoardingHouseId = listing.HouseId,
                Month = 5,
                Year = 2026,
                ElectricityAmount = 300_000,
                WaterAmount = 100_000,
                OtherExpensesTotal = 100_000,
                TotalExpense = 500_000
            };
            db.BoardingHouseExpenses.Add(expense);

            // Create 1 pending appointment
            var appointment = new Appointment
            {
                RoomId = listing.RoomId,
                UserId = tenantId,
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(2),
                Status = RequestStatus.Pending
            };
            db.Appointments.Add(appointment);

            // Create 1 open maintenance request
            var maintenance = new MaintenanceRequest
            {
                LeaseId = lease.Id,
                RoomId = listing.RoomId,
                ReportedByUserId = tenantId,
                Category = MaintenanceCategory.Water,
                Description = "Vòi nước bị rò rỉ",
                Status = MaintenanceStatus.Open
            };
            db.MaintenanceRequests.Add(maintenance);

            await db.SaveChangesAsync();
            secondRoomId = room2.Id;
        }

        // 1. Check Revenue Stats
        var revResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/my/stats/revenue?year=2026",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, revResp.StatusCode);
        var revStats = await revResp.ReadAsync<RevenueStatsResponse>();
        Assert.Equal(2026, revStats.Year);
        Assert.Equal(3_500_000m, revStats.TotalRevenue);
        Assert.Equal(3_000_000m, revStats.TotalRentRevenue);
        Assert.Equal(500_000m, revStats.TotalUtilityRevenue);
        Assert.Equal(1, revStats.TotalPaidBills);

        var month5Rev = revStats.MonthlyBreakdown.First(m => m.Month == 5);
        Assert.Equal(3_500_000m, month5Rev.Revenue);

        // 2. Check Revenue Years
        var yearsResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/my/stats/revenue/years",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, yearsResp.StatusCode);
        var years = await yearsResp.ReadAsync<RevenueYearsResponse>();
        Assert.Contains(2026, years.Years);

        // 3. Check Occupancy Stats
        var occResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/my/stats/occupancy",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, occResp.StatusCode);
        var occStats = await occResp.ReadAsync<OccupancyStatsResponse>();
        Assert.Equal(2, occStats.TotalRooms);
        Assert.Equal(1, occStats.RentedRooms);
        Assert.Equal(1, occStats.VacantRooms);
        Assert.Equal(50.0, occStats.OverallOccupancyRate);

        // 4. Check Profit Stats
        var profitResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/my/stats/profit?year=2026",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, profitResp.StatusCode);
        var profitStats = await profitResp.ReadAsync<ProfitStatsResponse>();
        Assert.Equal(3_500_000m, profitStats.TotalRevenue);
        Assert.Equal(500_000m, profitStats.TotalExpense);
        Assert.Equal(3_000_000m, profitStats.TotalNetProfit);

        var month5Profit = profitStats.MonthlyBreakdown.First(m => m.Month == 5);
        Assert.Equal(3_500_000m, month5Profit.Revenue);
        Assert.Equal(500_000m, month5Profit.Expense);
        Assert.Equal(3_000_000m, month5Profit.NetProfit);

        // 5. Check Dashboard Summary
        var summaryResp = await _client.SendAsync(
            HttpMethod.Get,
            "/api/v1/my/stats/summary",
            listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);
        var summary = await summaryResp.ReadAsync<DashboardSummaryResponse>();
        Assert.Equal(1, summary.TotalBoardingHouses);
        Assert.Equal(2, summary.TotalRooms);
        Assert.Equal(1, summary.OccupiedRooms);
        Assert.Equal(1, summary.VacantRooms);
        Assert.Equal(50.0, summary.OccupancyRate);
        Assert.Equal(1, summary.ActiveLeases);
        Assert.Equal(1, summary.PendingAppointments);
        Assert.Equal(1, summary.PendingMaintenanceRequests);
    }
}
