using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Bills;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class BillIssuanceFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public BillIssuanceFlowTests(PostgresFixture postgres) => _postgres = postgres;

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
    public async Task Room_additional_fees_can_be_created_updated_and_deleted()
    {
        var held = await _app.PaidDepositAsync(_client);
        (await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken)).EnsureSuccessStatusCode();

        // 1. Create fee
        var createResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/rooms/{held.Listing.RoomId}/additional-fees",
            held.Listing.OwnerToken,
            new CreateRoomAdditionalFeeRequest("Internet Fee", 150000, 9, 2026));

        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var fee = await createResp.ReadAsync<RoomAdditionalFeeResponse>();
        Assert.Equal("Internet Fee", fee.FeeName);
        Assert.Equal(150000, fee.FeeAmount);

        // 2. List fees
        var listResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/rooms/{held.Listing.RoomId}/additional-fees?month=9&year=2026",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var fees = await listResp.ReadAsync<IReadOnlyList<RoomAdditionalFeeResponse>>();
        Assert.Contains(fees, f => f.Id == fee.Id);

        // 3. Update fee
        var updateResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/rooms/{held.Listing.RoomId}/additional-fees/{fee.Id}",
            held.Listing.OwnerToken,
            new UpdateRoomAdditionalFeeRequest("High-Speed Internet", 180000));
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.ReadAsync<RoomAdditionalFeeResponse>();
        Assert.Equal("High-Speed Internet", updated.FeeName);
        Assert.Equal(180000, updated.FeeAmount);

        // 4. Delete fee
        var deleteResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/rooms/{held.Listing.RoomId}/additional-fees/{fee.Id}",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task Preview_and_create_bill_enforces_invariants_and_splits_among_tenants()
    {
        var held = await _app.PaidDepositAsync(_client);
        var confirmResp = await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken);
        var lease = await confirmResp.ReadAsync<LeaseResponse>();

        // Set house to DormStyle and add 2 co-tenants (total 3 live tenants)
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var house = await db.BoardingHouses.FirstAsync(b => b.Id == held.Listing.HouseId);
            house.Type = BoardingHouseType.DormStyle;
            house.ElectricityUnitPrice = 3500;
            house.WaterUnitPrice = 15000;
            var roomType = await db.RoomTypes.FirstAsync(rt => rt.Id == held.Listing.RoomTypeId);
            roomType.MaxOccupants = 4;
            await db.SaveChangesAsync();
        }

        (await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/leases/{lease.Id}/tenants",
            held.Listing.OwnerToken,
            new AddLeaseTenantRequest("Co-tenant Alpha", "0911111111", "001"))).EnsureSuccessStatusCode();

        (await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/leases/{lease.Id}/tenants",
            held.Listing.OwnerToken,
            new AddLeaseTenantRequest("Co-tenant Beta", "0922222222", "002"))).EnsureSuccessStatusCode();

        // Add additional fee
        (await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/rooms/{held.Listing.RoomId}/additional-fees",
            held.Listing.OwnerToken,
            new CreateRoomAdditionalFeeRequest("Trash Collection", 50000, 9, 2026))).EnsureSuccessStatusCode();

        // 1. Preview Bill
        var previewResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/bills/preview",
            held.Listing.OwnerToken,
            new PreviewBillRequest(held.Listing.RoomId, 9, 2026, 100, 20));

        Assert.Equal(HttpStatusCode.OK, previewResp.StatusCode);
        var preview = await previewResp.ReadAsync<BillResponse>();

        // Invariant §9.5: TotalAmount = Rent + Electricity + Water + AdditionalFees
        var expectedElec = 100 * 3500;
        var expectedWater = 20 * 15000;
        var expectedAddFee = 50000;
        var expectedTotal = lease.MonthlyRent + expectedElec + expectedWater + expectedAddFee;

        Assert.Equal(expectedTotal, preview.TotalAmount);
        Assert.Equal(expectedElec, preview.ElectricityAmount);
        Assert.Equal(expectedWater, preview.WaterAmount);
        Assert.Equal(expectedAddFee, preview.AdditionalFeeTotal);

        // Invariant §9.6: 3 tenants split exact to 1 VND, remainder to primary tenant
        Assert.Equal(3, preview.TenantSplits.Count);
        var totalSplitSum = preview.TenantSplits.Sum(s => s.Amount);
        Assert.Equal(expectedTotal, totalSplitSum);

        var primarySplit = preview.TenantSplits.First(s => s.IsPrimary);
        var otherSplits = preview.TenantSplits.Where(s => !s.IsPrimary).ToList();
        Assert.True(primarySplit.Amount >= otherSplits[0].Amount);

        // 2. Create Bill (Issued)
        var createResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/bills",
            held.Listing.OwnerToken,
            new CreateBillRequest(held.Listing.RoomId, 9, 2026, 100, 20));

        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var bill = await createResp.ReadAsync<BillResponse>();
        Assert.Equal(BillStatus.Issued, bill.Status);
        Assert.Equal(expectedTotal, bill.TotalAmount);

        // Room meter readings advanced (§3.4)
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var room = await db.Rooms.FirstAsync(r => r.Id == held.Listing.RoomId);
            Assert.Equal(100, room.CurrentElectricityReading);
            Assert.Equal(20, room.CurrentWaterReading);
        }

        // Invariant §9.4: Duplicate bill for same (RoomId, Month, Year) is forbidden
        var dupResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/bills",
            held.Listing.OwnerToken,
            new CreateBillRequest(held.Listing.RoomId, 9, 2026, 120, 25));

        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);
        Assert.Equal("error.bill.already_exists_for_period", await dupResp.ReadCodeAsync());

        // Invariant §9.9: Meter reading cannot go backwards
        var backwardResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/bills",
            held.Listing.OwnerToken,
            new CreateBillRequest(held.Listing.RoomId, 10, 2026, 90, 25)); // elec 90 < 100

        Assert.Equal(HttpStatusCode.UnprocessableEntity, backwardResp.StatusCode);
        Assert.Equal("error.bill.reading_went_backwards", await backwardResp.ReadCodeAsync());
    }

    [Fact]
    public async Task Bill_PDF_can_be_downloaded_and_contains_valid_pdf_stream()
    {
        var held = await _app.PaidDepositAsync(_client);
        (await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken)).EnsureSuccessStatusCode();

        var billResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/bills",
            held.Listing.OwnerToken,
            new CreateBillRequest(held.Listing.RoomId, 8, 2026, 50, 10));

        var bill = await billResp.ReadAsync<BillResponse>();

        var pdfResp = await _client.SendAsync(
            HttpMethod.Get,
            $"/api/v1/bills/{bill.Id}/pdf",
            held.TenantToken);

        Assert.Equal(HttpStatusCode.OK, pdfResp.StatusCode);
        Assert.Equal("application/pdf", pdfResp.Content.Headers.ContentType?.MediaType);

        var bytes = await pdfResp.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        // PDF files begin with %PDF-
        var header = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task Draft_bill_lifecycle_and_cancellation_releases_fees_and_reverts_meters()
    {
        var held = await _app.PaidDepositAsync(_client);
        (await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken)).EnsureSuccessStatusCode();

        await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/rooms/{held.Listing.RoomId}/additional-fees",
            held.Listing.OwnerToken,
            new CreateRoomAdditionalFeeRequest("Maintenance", 30000, 7, 2026));

        // Create draft bill
        var draftResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/bills",
            held.Listing.OwnerToken,
            new CreateBillRequest(held.Listing.RoomId, 7, 2026, 40, 8, Status: BillStatus.Draft));

        Assert.Equal(HttpStatusCode.OK, draftResp.StatusCode);
        var draftBill = await draftResp.ReadAsync<BillResponse>();
        Assert.Equal(BillStatus.Draft, draftBill.Status);

        // Update draft
        var updateResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/bills/{draftBill.Id}",
            held.Listing.OwnerToken,
            new UpdateDraftBillRequest(45, 9));
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updatedDraft = await updateResp.ReadAsync<BillResponse>();
        Assert.Equal(45, updatedDraft.ElectricityNew);

        // Cancel bill
        var cancelResp = await _client.SendAsync(
            HttpMethod.Put,
            $"/api/v1/bills/{draftBill.Id}/cancel",
            held.Listing.OwnerToken);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var cancelledBill = await cancelResp.ReadAsync<BillResponse>();
        Assert.Equal(BillStatus.Cancelled, cancelledBill.Status);

        // Verify additional fee is unlinked
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
        var fee = await db.RoomAdditionalFees.FirstAsync(f => f.RoomId == held.Listing.RoomId && f.Month == 7);
        Assert.Null(fee.PaymentBillId);

        // Verify room meter was restored
        var room = await db.Rooms.FirstAsync(r => r.Id == held.Listing.RoomId);
        Assert.Equal(0, room.CurrentElectricityReading);
        Assert.Equal(0, room.CurrentWaterReading);
    }

    [Fact]
    public async Task Sweep_bill_reminders_transitions_overdue_and_notifies_due_soon()
    {
        var held = await _app.PaidDepositAsync(_client);
        (await _client.SendAsync(
            HttpMethod.Post,
            $"/api/v1/deposits/{held.Deposit.Id}/confirm-lease",
            held.Listing.OwnerToken)).EnsureSuccessStatusCode();

        var billResp = await _client.SendAsync(
            HttpMethod.Post,
            "/api/v1/bills",
            held.Listing.OwnerToken,
            new CreateBillRequest(held.Listing.RoomId, 6, 2026, 30, 5));
        var bill = await billResp.ReadAsync<BillResponse>();

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var b = await db.PaymentBills.FirstAsync(x => x.Id == bill.Id);
            // Set due date in 3 days
            b.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
            await db.SaveChangesAsync();

            var sweeper = scope.ServiceProvider.GetRequiredService<SweepBillRemindersHandler>();
            var count = await sweeper.HandleAsync();
            Assert.True(count >= 1);

            // Set due date in past
            b.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            await db.SaveChangesAsync();

            await sweeper.HandleAsync();
            var overdueBill = await db.PaymentBills.FirstAsync(x => x.Id == bill.Id);
            Assert.Equal(BillStatus.Overdue, overdueBill.Status);
        }
    }
}
