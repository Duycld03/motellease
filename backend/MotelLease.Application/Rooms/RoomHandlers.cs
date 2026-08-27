using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Rooms.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Domain.Rooms;

namespace MotelLease.Application.Rooms;

internal static class RoomRules
{
    internal static IQueryable<RoomResponse> Project(IQueryable<Room> query) =>
        query.Select(r => new RoomResponse(
            r.Id,
            r.BoardingHouseId,
            r.RoomTypeId,
            r.RoomType.TypeName,
            r.RoomType.Price,
            r.RoomNumber,
            r.Status,
            r.Description,
            r.CurrentElectricityReading,
            r.CurrentWaterReading,
            r.UpdatedAt));

    internal static async Task<RoomResponse> LoadAsync(
        IAppDbContext database,
        Guid roomId,
        CancellationToken cancellationToken) =>
        await Project(database.Rooms.AsNoTracking().Where(r => r.Id == roomId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Room.NotFound);

    /// <summary>
    /// Loads a room reached by its own id and checks the caller may act on its property. The
    /// route carries no boarding house id, so the house comes from the room itself.
    /// </summary>
    internal static async Task<Room> RequireWritableAsync(
        IAppDbContext database,
        BoardingHouseAccess access,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await database.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        await access.RequireStaffOrOwnerAsync(room.BoardingHouseId, cancellationToken);

        return room;
    }

    /// <summary>
    /// The partial unique index is the authority; this check exists so the caller gets a clear
    /// conflict instead of a constraint violation.
    /// </summary>
    internal static async Task EnsureNumberIsFreeAsync(
        IAppDbContext database,
        Guid boardingHouseId,
        string roomNumber,
        Guid? excludingRoomId,
        CancellationToken cancellationToken)
    {
        var taken = await database.Rooms.AnyAsync(
            r => r.BoardingHouseId == boardingHouseId
                 && r.RoomNumber == roomNumber
                 && (excludingRoomId == null || r.Id != excludingRoomId),
            cancellationToken);

        if (taken)
        {
            throw new ConflictException(MessageKeys.Room.NumberTaken, roomNumber);
        }
    }
}

/// <summary>
/// GET /my/boarding-houses/{id}/rooms, optionally narrowed to one status so the owner can pull
/// up the vacant ones. Unpaged for the same reason as the room types: the set is per property.
/// </summary>
public sealed class ListRoomsHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<IReadOnlyList<RoomResponse>> HandleAsync(
        Guid boardingHouseId,
        RoomStatus? status,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        var query = database.Rooms
            .AsNoTracking()
            .Where(r => r.BoardingHouseId == house.Id);

        if (status is { } wanted)
        {
            query = query.Where(r => r.Status == wanted);
        }

        return await RoomRules
            .Project(query.OrderBy(r => r.RoomNumber))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// POST /my/boarding-houses/{id}/rooms. A new room starts Available with meter readings at zero;
/// the readings are corrected through the meter-readings endpoint.
/// </summary>
public sealed class CreateRoomHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<RoomResponse> HandleAsync(
        Guid boardingHouseId,
        SaveRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);
        var roomNumber = request.RoomNumber.Trim();

        // A type from another property would price this room off someone else's listing.
        if (!await database.RoomTypes.AnyAsync(
                t => t.Id == request.RoomTypeId && t.BoardingHouseId == house.Id,
                cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.Room.RoomTypeFromAnotherHouse);
        }

        await RoomRules.EnsureNumberIsFreeAsync(
            database, house.Id, roomNumber, excludingRoomId: null, cancellationToken);

        var room = new Room
        {
            BoardingHouseId = house.Id,
            RoomTypeId = request.RoomTypeId,
            RoomNumber = roomNumber,
            Status = RoomStatus.Available,
            Description = request.Description?.Trim()
        };

        database.Rooms.Add(room);

        await database.SaveChangesAsync(cancellationToken);

        return await RoomRules.LoadAsync(database, room.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /my/boarding-houses/{id}/rooms/{roomId}. The status is not editable here — it has its own
/// endpoint because most of its values are not a matter of choice.
/// </summary>
public sealed class UpdateRoomHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<RoomResponse> HandleAsync(
        Guid boardingHouseId,
        Guid roomId,
        SaveRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);
        var roomNumber = request.RoomNumber.Trim();

        var room = await database.Rooms.FirstOrDefaultAsync(
            r => r.Id == roomId && r.BoardingHouseId == house.Id,
            cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        if (!await database.RoomTypes.AnyAsync(
                t => t.Id == request.RoomTypeId && t.BoardingHouseId == house.Id,
                cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.Room.RoomTypeFromAnotherHouse);
        }

        await RoomRules.EnsureNumberIsFreeAsync(
            database, house.Id, roomNumber, room.Id, cancellationToken);

        room.RoomTypeId = request.RoomTypeId;
        room.RoomNumber = roomNumber;
        room.Description = request.Description?.Trim();

        await database.SaveChangesAsync(cancellationToken);

        return await RoomRules.LoadAsync(database, room.Id, cancellationToken);
    }
}

/// <summary>
/// DELETE /my/boarding-houses/{id}/rooms/{roomId}. Soft delete, which also frees the room number
/// for reuse: the unique index is partial on IsDeleted = false.
/// </summary>
public sealed class DeleteRoomHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid boardingHouseId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        var room = await database.Rooms.FirstOrDefaultAsync(
            r => r.Id == roomId && r.BoardingHouseId == house.Id,
            cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        // Asked of the lease and deposit rows rather than of room.Status: the rows are the
        // source of truth for occupancy (docs/domain-rules.md §9.3).
        var leased = await database.Leases.AnyAsync(
            l => l.RoomId == room.Id
                 && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring),
            cancellationToken);

        var held = await database.Deposits.AnyAsync(
            d => d.RoomId == room.Id
                 && (d.Status == DepositStatus.Accepted || d.Status == DepositStatus.Paid),
            cancellationToken);

        if (leased || held)
        {
            throw new ConflictException(MessageKeys.Room.InUse);
        }

        room.IsDeleted = true;

        await database.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// PUT /my/rooms/{roomId}/status. Taking a room out of service and putting it back is the whole
/// range of manual moves; Occupied and Reserved follow from lease and deposit rows, so they are
/// refused here (see <see cref="RoomStatusPolicy"/>).
/// </summary>
public sealed class UpdateRoomStatusHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<RoomResponse> HandleAsync(
        Guid roomId,
        UpdateRoomStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await RoomRules.RequireWritableAsync(database, access, roomId, cancellationToken);

        if (!RoomStatusPolicy.IsManuallySettable(request.Status))
        {
            throw new BusinessRuleException(MessageKeys.Room.StatusNotManuallySettable);
        }

        if (!RoomStatusPolicy.CanBeChangedFrom(room.Status))
        {
            throw new ConflictException(MessageKeys.Room.StatusLockedByOccupancy);
        }

        room.Status = request.Status;

        await database.SaveChangesAsync(cancellationToken);

        return await RoomRules.LoadAsync(database, room.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /my/rooms/{roomId}/meter-readings. One current figure per meter, which next month's bill
/// reads as its opening value (docs/domain-rules.md §3). A reading that moves backwards would
/// produce a negative quantity, so it is refused rather than stored.
/// </summary>
public sealed class UpdateMeterReadingsHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<RoomResponse> HandleAsync(
        Guid roomId,
        UpdateMeterReadingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await RoomRules.RequireWritableAsync(database, access, roomId, cancellationToken);

        if (request.ElectricityReading < room.CurrentElectricityReading
            || request.WaterReading < room.CurrentWaterReading)
        {
            throw new BusinessRuleException(
                MessageKeys.Room.ReadingWentBackwards,
                room.CurrentElectricityReading,
                room.CurrentWaterReading);
        }

        room.CurrentElectricityReading = request.ElectricityReading;
        room.CurrentWaterReading = request.WaterReading;

        await database.SaveChangesAsync(cancellationToken);

        return await RoomRules.LoadAsync(database, room.Id, cancellationToken);
    }
}
