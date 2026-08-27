using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Bills;

internal static class BillRules
{
    internal static IReadOnlyList<TenantBillSplitResponse> ComputeSplits(
        decimal totalAmount,
        IEnumerable<LeaseTenant> tenants)
    {
        var liveTenants = tenants
            .Where(t => t.MovedOutAt == null)
            .OrderByDescending(t => t.IsPrimary)
            .ThenBy(t => t.MovedInAt)
            .ToList();

        if (liveTenants.Count == 0)
        {
            return [];
        }

        var baseShare = Math.Floor(totalAmount / liveTenants.Count);
        var remainder = totalAmount - (baseShare * liveTenants.Count);

        return liveTenants.Select(t => new TenantBillSplitResponse(
            t.Id,
            t.UserId,
            t.FullName,
            t.IsPrimary,
            t.IsPrimary ? baseShare + remainder : baseShare)).ToList();
    }

    internal static async Task<BillResponse> LoadAsync(
        IAppDbContext database,
        Guid billId,
        CancellationToken cancellationToken)
    {
        var bill = await database.PaymentBills
            .AsNoTracking()
            .Include(b => b.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(b => b.AdditionalFees)
            .Include(b => b.Lease)
            .ThenInclude(l => l.Tenants)
            .FirstOrDefaultAsync(b => b.Id == billId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Bill.NotFound);

        var splits = ComputeSplits(bill.TotalAmount, bill.Lease.Tenants);

        var feeResponses = bill.AdditionalFees.Select(f => new RoomAdditionalFeeResponse(
            f.Id,
            f.RoomId,
            f.PaymentBillId,
            f.FeeName,
            f.FeeAmount,
            f.Month,
            f.Year,
            f.CreatedAt)).ToList();

        return new BillResponse(
            bill.Id,
            bill.LeaseId,
            bill.RoomId,
            bill.Room.RoomNumber,
            bill.Room.BoardingHouseId,
            bill.Room.BoardingHouse.Name,
            bill.Month,
            bill.Year,
            bill.RentAmount,
            bill.ElectricityOld,
            bill.ElectricityNew,
            bill.ElectricityQty,
            bill.ElectricityUnitPrice,
            bill.ElectricityAmount,
            bill.WaterOld,
            bill.WaterNew,
            bill.WaterQty,
            bill.WaterUnitPrice,
            bill.WaterAmount,
            bill.AdditionalFeeTotal,
            bill.TotalAmount,
            bill.Status,
            bill.IssuedAt,
            bill.DueDate,
            bill.PaidAt,
            feeResponses,
            splits,
            bill.CreatedAt);
    }
}

public sealed class ListRoomAdditionalFeesHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<IReadOnlyList<RoomAdditionalFeeResponse>> HandleAsync(
        Guid roomId,
        int? month = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var room = await database.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        var query = database.RoomAdditionalFees.AsNoTracking().Where(f => f.RoomId == roomId);
        if (month.HasValue) query = query.Where(f => f.Month == month.Value);
        if (year.HasValue) query = query.Where(f => f.Year == year.Value);

        return await query
            .OrderBy(f => f.Year)
            .ThenBy(f => f.Month)
            .ThenBy(f => f.CreatedAt)
            .Select(f => new RoomAdditionalFeeResponse(
                f.Id,
                f.RoomId,
                f.PaymentBillId,
                f.FeeName,
                f.FeeAmount,
                f.Month,
                f.Year,
                f.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public sealed class CreateRoomAdditionalFeeHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<RoomAdditionalFeeResponse> HandleAsync(
        Guid roomId,
        CreateRoomAdditionalFeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await database.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        var fee = new RoomAdditionalFee
        {
            RoomId = roomId,
            FeeName = request.FeeName.Trim(),
            FeeAmount = request.FeeAmount,
            Month = request.Month,
            Year = request.Year,
            PaymentBillId = null
        };

        database.RoomAdditionalFees.Add(fee);
        await database.SaveChangesAsync(cancellationToken);

        return new RoomAdditionalFeeResponse(
            fee.Id,
            fee.RoomId,
            fee.PaymentBillId,
            fee.FeeName,
            fee.FeeAmount,
            fee.Month,
            fee.Year,
            fee.CreatedAt);
    }
}

public sealed class UpdateRoomAdditionalFeeHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<RoomAdditionalFeeResponse> HandleAsync(
        Guid roomId,
        Guid feeId,
        UpdateRoomAdditionalFeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await database.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        var fee = await database.RoomAdditionalFees
            .FirstOrDefaultAsync(f => f.Id == feeId && f.RoomId == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.AdditionalFee.NotFound);

        if (fee.PaymentBillId.HasValue)
        {
            throw new BusinessRuleException(MessageKeys.AdditionalFee.AlreadyBilled);
        }

        fee.FeeName = request.FeeName.Trim();
        fee.FeeAmount = request.FeeAmount;

        await database.SaveChangesAsync(cancellationToken);

        return new RoomAdditionalFeeResponse(
            fee.Id,
            fee.RoomId,
            fee.PaymentBillId,
            fee.FeeName,
            fee.FeeAmount,
            fee.Month,
            fee.Year,
            fee.CreatedAt);
    }
}

public sealed class DeleteRoomAdditionalFeeHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid roomId,
        Guid feeId,
        CancellationToken cancellationToken = default)
    {
        var room = await database.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        var fee = await database.RoomAdditionalFees
            .FirstOrDefaultAsync(f => f.Id == feeId && f.RoomId == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.AdditionalFee.NotFound);

        if (fee.PaymentBillId.HasValue)
        {
            throw new BusinessRuleException(MessageKeys.AdditionalFee.AlreadyBilled);
        }

        database.RoomAdditionalFees.Remove(fee);
        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class PreviewBillHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<BillResponse> HandleAsync(
        PreviewBillRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await database.Rooms
            .Include(r => r.BoardingHouse)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        if (request.ElectricityNew < room.CurrentElectricityReading ||
            request.WaterNew < room.CurrentWaterReading)
        {
            throw new BusinessRuleException(
                MessageKeys.Bill.ReadingWentBackwards,
                room.CurrentElectricityReading,
                room.CurrentWaterReading);
        }

        var lease = await database.Leases
            .Include(l => l.Tenants)
            .Where(l => l.RoomId == room.Id &&
                        (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring))
            .OrderByDescending(l => l.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException(
                MessageKeys.Bill.NoActiveLease, request.Month, request.Year);

        var unbilledFees = await database.RoomAdditionalFees
            .AsNoTracking()
            .Where(f => f.RoomId == room.Id &&
                        f.Month == request.Month &&
                        f.Year == request.Year &&
                        f.PaymentBillId == null)
            .ToListAsync(cancellationToken);

        var eleOld = room.CurrentElectricityReading;
        var waterOld = room.CurrentWaterReading;
        var eleQty = request.ElectricityNew - eleOld;
        var waterQty = request.WaterNew - waterOld;
        var eleAmount = eleQty * room.BoardingHouse.ElectricityUnitPrice;
        var waterAmount = waterQty * room.BoardingHouse.WaterUnitPrice;
        var addFeeTotal = unbilledFees.Sum(f => f.FeeAmount);
        var rentAmount = lease.MonthlyRent;
        var totalAmount = rentAmount + eleAmount + waterAmount + addFeeTotal;

        var feeResponses = unbilledFees.Select(f => new RoomAdditionalFeeResponse(
            f.Id, f.RoomId, f.PaymentBillId, f.FeeName, f.FeeAmount, f.Month, f.Year, f.CreatedAt)).ToList();

        var splits = BillRules.ComputeSplits(totalAmount, lease.Tenants);

        return new BillResponse(
            Guid.Empty,
            lease.Id,
            room.Id,
            room.RoomNumber,
            room.BoardingHouseId,
            room.BoardingHouse.Name,
            request.Month,
            request.Year,
            rentAmount,
            eleOld,
            request.ElectricityNew,
            eleQty,
            room.BoardingHouse.ElectricityUnitPrice,
            eleAmount,
            waterOld,
            request.WaterNew,
            waterQty,
            room.BoardingHouse.WaterUnitPrice,
            waterAmount,
            addFeeTotal,
            totalAmount,
            BillStatus.Draft,
            null,
            null,
            null,
            feeResponses,
            splits,
            DateTimeOffset.UtcNow);
    }
}

public sealed class CreateBillHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    NotificationDispatcher notifications,
    TimeProvider time)
{
    public async Task<BillResponse> HandleAsync(
        CreateBillRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await database.Rooms
            .Include(r => r.BoardingHouse)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        var existing = await database.PaymentBills.AnyAsync(
            b => b.RoomId == room.Id && b.Month == request.Month && b.Year == request.Year,
            cancellationToken);

        if (existing)
        {
            throw new ConflictException(
                MessageKeys.Bill.AlreadyExistsForPeriod,
                room.RoomNumber,
                request.Month,
                request.Year);
        }

        if (request.ElectricityNew < room.CurrentElectricityReading ||
            request.WaterNew < room.CurrentWaterReading)
        {
            throw new BusinessRuleException(
                MessageKeys.Bill.ReadingWentBackwards,
                room.CurrentElectricityReading,
                room.CurrentWaterReading);
        }

        var lease = await database.Leases
            .Include(l => l.Tenants)
            .Where(l => l.RoomId == room.Id &&
                        (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring))
            .OrderByDescending(l => l.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException(
                MessageKeys.Bill.NoActiveLease, request.Month, request.Year);

        var unbilledFees = await database.RoomAdditionalFees
            .Where(f => f.RoomId == room.Id &&
                        f.Month == request.Month &&
                        f.Year == request.Year &&
                        f.PaymentBillId == null)
            .ToListAsync(cancellationToken);

        var eleOld = room.CurrentElectricityReading;
        var waterOld = room.CurrentWaterReading;
        var eleQty = request.ElectricityNew - eleOld;
        var waterQty = request.WaterNew - waterOld;
        var eleAmount = eleQty * room.BoardingHouse.ElectricityUnitPrice;
        var waterAmount = waterQty * room.BoardingHouse.WaterUnitPrice;
        var addFeeTotal = unbilledFees.Sum(f => f.FeeAmount);
        var rentAmount = lease.MonthlyRent;
        var totalAmount = rentAmount + eleAmount + waterAmount + addFeeTotal;

        var now = time.GetUtcNow();
        var dueDate = request.DueDate ?? DateOnly.FromDateTime(now.AddDays(7).DateTime);

        var bill = new PaymentBill
        {
            LeaseId = lease.Id,
            RoomId = room.Id,
            Month = request.Month,
            Year = request.Year,
            RentAmount = rentAmount,
            ElectricityOld = eleOld,
            ElectricityNew = request.ElectricityNew,
            ElectricityQty = eleQty,
            ElectricityUnitPrice = room.BoardingHouse.ElectricityUnitPrice,
            ElectricityAmount = eleAmount,
            WaterOld = waterOld,
            WaterNew = request.WaterNew,
            WaterQty = waterQty,
            WaterUnitPrice = room.BoardingHouse.WaterUnitPrice,
            WaterAmount = waterAmount,
            AdditionalFeeTotal = addFeeTotal,
            TotalAmount = totalAmount,
            Status = request.Status,
            IssuedAt = request.Status == BillStatus.Issued ? now : null,
            DueDate = request.Status == BillStatus.Issued ? dueDate : null
        };

        await using var scope = await database.BeginTransactionAsync(cancellationToken);

        database.PaymentBills.Add(bill);

        // Advance room meter readings (§3.4)
        room.CurrentElectricityReading = request.ElectricityNew;
        room.CurrentWaterReading = request.WaterNew;

        foreach (var fee in unbilledFees)
        {
            fee.PaymentBill = bill;
        }

        if (request.Status == BillStatus.Issued)
        {
            notifications.Queue(
                lease.PrimaryTenantUserId,
                NotificationType.BillIssued,
                new
                {
                    month = request.Month,
                    year = request.Year,
                    roomNumber = room.RoomNumber,
                    boardingHouseName = room.BoardingHouse.Name,
                    amount = totalAmount
                },
                linkUrl: $"/bills/{bill.Id}");
        }

        await database.SaveChangesAsync(cancellationToken);
        await scope.CommitAsync(cancellationToken);

        if (request.Status == BillStatus.Issued)
        {
            await notifications.DeliverAsync(cancellationToken);
        }

        return await BillRules.LoadAsync(database, bill.Id, cancellationToken);
    }
}

public sealed class ListBillsHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<BillResponse>> HandleAsync(
        BillStatus? status = null,
        int? month = null,
        int? year = null,
        Guid? boardingHouseId = null,
        Guid? roomId = null,
        int page = 1,
        int pageSize = Paged.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var query = database.PaymentBills
            .AsNoTracking()
            .Include(b => b.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(b => b.AdditionalFees)
            .Include(b => b.Lease)
            .ThenInclude(l => l.Tenants)
            .AsQueryable();

        if (currentUser.Role == UserRole.Tenant)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(b =>
                b.Lease.PrimaryTenantUserId == userId ||
                b.Lease.Tenants.Any(t => t.UserId == userId));
        }
        else if (boardingHouseId.HasValue)
        {
            await access.RequireStaffOrOwnerAsync(boardingHouseId.Value, cancellationToken);
            query = query.Where(b => b.Room.BoardingHouseId == boardingHouseId.Value);
        }
        else
        {
            var managedHouseIds = access.Managed().Select(b => b.Id);
            query = query.Where(b => managedHouseIds.Contains(b.Room.BoardingHouseId));
        }

        if (status.HasValue) query = query.Where(b => b.Status == status.Value);
        if (month.HasValue) query = query.Where(b => b.Month == month.Value);
        if (year.HasValue) query = query.Where(b => b.Year == year.Value);
        if (roomId.HasValue) query = query.Where(b => b.RoomId == roomId.Value);

        var projected = query.OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new BillResponse(
                b.Id,
                b.LeaseId,
                b.RoomId,
                b.Room.RoomNumber,
                b.Room.BoardingHouseId,
                b.Room.BoardingHouse.Name,
                b.Month,
                b.Year,
                b.RentAmount,
                b.ElectricityOld,
                b.ElectricityNew,
                b.ElectricityQty,
                b.ElectricityUnitPrice,
                b.ElectricityAmount,
                b.WaterOld,
                b.WaterNew,
                b.WaterQty,
                b.WaterUnitPrice,
                b.WaterAmount,
                b.AdditionalFeeTotal,
                b.TotalAmount,
                b.Status,
                b.IssuedAt,
                b.DueDate,
                b.PaidAt,
                b.AdditionalFees.Select(f => new RoomAdditionalFeeResponse(
                    f.Id, f.RoomId, f.PaymentBillId, f.FeeName, f.FeeAmount, f.Month, f.Year, f.CreatedAt)).ToList(),
                new List<TenantBillSplitResponse>(),
                b.CreatedAt));

        var pagedResult = await Paged.FromAsync(projected, page, pageSize, cancellationToken);

        // Populate tenant splits in-memory for each item on the page
        var itemsWithSplits = new List<BillResponse>();
        foreach (var item in pagedResult.Items)
        {
            var bill = await database.PaymentBills
                .AsNoTracking()
                .Include(b => b.Lease)
                .ThenInclude(l => l.Tenants)
                .FirstAsync(b => b.Id == item.Id, cancellationToken);

            var splits = BillRules.ComputeSplits(item.TotalAmount, bill.Lease.Tenants);
            itemsWithSplits.Add(item with { TenantSplits = splits });
        }

        return new PagedResponse<BillResponse>(
            itemsWithSplits,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.Total,
            pagedResult.TotalPages);
    }
}

public sealed class GetBillHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<BillResponse> HandleAsync(
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var bill = await database.PaymentBills
            .AsNoTracking()
            .Where(b => b.Id == billId)
            .Select(b => new
            {
                b.Room.BoardingHouseId,
                b.Lease.PrimaryTenantUserId,
                TenantUserIds = b.Lease.Tenants.Where(t => t.UserId.HasValue).Select(t => t.UserId!.Value).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Bill.NotFound);

        if (currentUser.Role == UserRole.Tenant)
        {
            var userId = currentUser.RequireUserId();
            if (bill.PrimaryTenantUserId != userId && !bill.TenantUserIds.Contains(userId))
            {
                throw new ForbiddenException(MessageKeys.Bill.NotYours);
            }
        }
        else
        {
            await access.RequireStaffOrOwnerAsync(bill.BoardingHouseId, cancellationToken);
        }

        return await BillRules.LoadAsync(database, billId, cancellationToken);
    }
}

public sealed class UpdateDraftBillHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<BillResponse> HandleAsync(
        Guid billId,
        UpdateDraftBillRequest request,
        CancellationToken cancellationToken = default)
    {
        var bill = await database.PaymentBills
            .Include(b => b.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(b => b.AdditionalFees)
            .FirstOrDefaultAsync(b => b.Id == billId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Bill.NotFound);

        await access.RequireStaffOrOwnerAsync(bill.Room.BoardingHouseId, cancellationToken);

        if (bill.Status != BillStatus.Draft)
        {
            throw new BusinessRuleException(MessageKeys.Bill.NotDraft);
        }

        if (request.ElectricityNew < bill.ElectricityOld || request.WaterNew < bill.WaterOld)
        {
            throw new BusinessRuleException(
                MessageKeys.Bill.ReadingWentBackwards,
                bill.ElectricityOld,
                bill.WaterOld);
        }

        var eleQty = request.ElectricityNew - bill.ElectricityOld;
        var waterQty = request.WaterNew - bill.WaterOld;
        var eleAmount = eleQty * bill.Room.BoardingHouse.ElectricityUnitPrice;
        var waterAmount = waterQty * bill.Room.BoardingHouse.WaterUnitPrice;
        var addFeeTotal = bill.AdditionalFees.Sum(f => f.FeeAmount);
        var totalAmount = bill.RentAmount + eleAmount + waterAmount + addFeeTotal;

        bill.ElectricityNew = request.ElectricityNew;
        bill.ElectricityQty = eleQty;
        bill.ElectricityAmount = eleAmount;
        bill.WaterNew = request.WaterNew;
        bill.WaterQty = waterQty;
        bill.WaterAmount = waterAmount;
        bill.TotalAmount = totalAmount;
        if (request.DueDate.HasValue) bill.DueDate = request.DueDate.Value;

        bill.Room.CurrentElectricityReading = request.ElectricityNew;
        bill.Room.CurrentWaterReading = request.WaterNew;

        await database.SaveChangesAsync(cancellationToken);

        return await BillRules.LoadAsync(database, bill.Id, cancellationToken);
    }
}

public sealed class IssueDraftBillHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    NotificationDispatcher notifications,
    TimeProvider time)
{
    public async Task<BillResponse> HandleAsync(
        Guid billId,
        IssueDraftBillRequest request,
        CancellationToken cancellationToken = default)
    {
        var bill = await database.PaymentBills
            .Include(b => b.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(b => b.Lease)
            .FirstOrDefaultAsync(b => b.Id == billId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Bill.NotFound);

        await access.RequireStaffOrOwnerAsync(bill.Room.BoardingHouseId, cancellationToken);

        if (bill.Status != BillStatus.Draft)
        {
            throw new BusinessRuleException(MessageKeys.Bill.NotDraft);
        }

        var now = time.GetUtcNow();
        bill.Status = BillStatus.Issued;
        bill.IssuedAt = now;
        bill.DueDate = request.DueDate;

        notifications.Queue(
            bill.Lease.PrimaryTenantUserId,
            NotificationType.BillIssued,
            new
            {
                month = bill.Month,
                year = bill.Year,
                roomNumber = bill.Room.RoomNumber,
                boardingHouseName = bill.Room.BoardingHouse.Name,
                amount = bill.TotalAmount
            },
            linkUrl: $"/bills/{bill.Id}");

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        return await BillRules.LoadAsync(database, bill.Id, cancellationToken);
    }
}

public sealed class CancelBillHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<BillResponse> HandleAsync(
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var bill = await database.PaymentBills
            .Include(b => b.Room)
            .Include(b => b.AdditionalFees)
            .FirstOrDefaultAsync(b => b.Id == billId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Bill.NotFound);

        await access.RequireStaffOrOwnerAsync(bill.Room.BoardingHouseId, cancellationToken);

        if (bill.Status == BillStatus.Paid || bill.Status == BillStatus.Cancelled)
        {
            throw new BusinessRuleException(MessageKeys.Bill.NotCancellable);
        }

        await using var scope = await database.BeginTransactionAsync(cancellationToken);

        bill.Status = BillStatus.Cancelled;

        foreach (var fee in bill.AdditionalFees)
        {
            fee.PaymentBillId = null;
        }

        // If this bill was the latest reading advance, restore previous readings
        if (bill.Room.CurrentElectricityReading == bill.ElectricityNew)
        {
            bill.Room.CurrentElectricityReading = bill.ElectricityOld;
        }

        if (bill.Room.CurrentWaterReading == bill.WaterNew)
        {
            bill.Room.CurrentWaterReading = bill.WaterOld;
        }

        await database.SaveChangesAsync(cancellationToken);
        await scope.CommitAsync(cancellationToken);

        return await BillRules.LoadAsync(database, bill.Id, cancellationToken);
    }
}

public sealed class GenerateBillPdfHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    IBillPdfGenerator pdfGenerator,
    ICurrentUser currentUser)
{
    public async Task<BillPdfResponse> HandleAsync(
        Guid billId,
        string language,
        CancellationToken cancellationToken = default)
    {
        var billResponse = await new GetBillHandler(database, access, currentUser)
            .HandleAsync(billId, cancellationToken);

        var bytes = pdfGenerator.Generate(billResponse, language);
        var fileName = $"Bill-{billResponse.RoomNumber}-{billResponse.Month:D2}{billResponse.Year}.pdf";

        return new BillPdfResponse(bytes, fileName, "application/pdf");
    }
}

