using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Domain.Rooms;

namespace MotelLease.Application.RoomTypes;

/// <summary>
/// Rules and projections shared by the four room-type handlers.
/// </summary>
internal static class RoomTypeRules
{
    internal static IQueryable<RoomTypeResponse> Project(IQueryable<RoomType> query) =>
        query.Select(t => new RoomTypeResponse(
            t.Id,
            t.BoardingHouseId,
            t.TypeName,
            t.Price,
            t.RoomSizeM2,
            t.MaxOccupants,
            t.Description,
            t.Rooms.Count,
            t.Facilities
                .OrderBy(f => f.Name)
                .Select(f => new FacilityResponse(f.Id, f.Name, f.CodeName, f.IconKey))
                .ToList()));

    /// <summary>
    /// docs/domain-rules.md §1: only a DormStyle house shares a room. Allowing a higher cap
    /// elsewhere would describe an occupancy the lease rules refuse to create.
    /// </summary>
    internal static void EnsureOccupancyFitsHouse(BoardingHouse house, int maxOccupants)
    {
        if (maxOccupants > 1 && !RoomOccupancyPolicy.AllowsSharing(house.Type))
        {
            throw new BusinessRuleException(MessageKeys.RoomType.SingleOccupantHouse);
        }
    }

    internal static async Task<List<Facility>> LoadFacilitiesAsync(
        IAppDbContext database,
        IReadOnlyList<Guid> facilityIds,
        CancellationToken cancellationToken)
    {
        var wanted = facilityIds.Distinct().ToList();

        if (wanted.Count == 0)
        {
            return [];
        }

        var facilities = await database.Facilities
            .Where(f => wanted.Contains(f.Id))
            .ToListAsync(cancellationToken);

        // A silently dropped facility would leave the owner believing the listing advertises
        // something it does not.
        return facilities.Count == wanted.Count
            ? facilities
            : throw new NotFoundException(MessageKeys.RoomType.FacilityNotFound);
    }

    internal static async Task<RoomTypeResponse> LoadAsync(
        IAppDbContext database,
        Guid roomTypeId,
        CancellationToken cancellationToken) =>
        await Project(database.RoomTypes.AsNoTracking().Where(t => t.Id == roomTypeId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.RoomType.NotFound);
}

/// <summary>
/// GET /my/boarding-houses/{id}/room-types. Unpaged: a property has a handful of types, and
/// paging a list that short would cost the caller a round trip for nothing.
/// </summary>
public sealed class ListRoomTypesHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<IReadOnlyList<RoomTypeResponse>> HandleAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        return await RoomTypeRules
            .Project(database.RoomTypes
                .AsNoTracking()
                .Where(t => t.BoardingHouseId == house.Id)
                .OrderBy(t => t.Price))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>POST /my/boarding-houses/{id}/room-types.</summary>
public sealed class CreateRoomTypeHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<RoomTypeResponse> HandleAsync(
        Guid boardingHouseId,
        SaveRoomTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        RoomTypeRules.EnsureOccupancyFitsHouse(house, request.MaxOccupants);

        var roomType = new RoomType
        {
            BoardingHouseId = house.Id,
            TypeName = request.TypeName.Trim(),
            Price = request.Price,
            RoomSizeM2 = request.RoomSizeM2,
            MaxOccupants = request.MaxOccupants,
            Description = request.Description?.Trim()
        };

        foreach (var facility in await RoomTypeRules.LoadFacilitiesAsync(
                     database, request.FacilityIds, cancellationToken))
        {
            roomType.Facilities.Add(facility);
        }

        database.RoomTypes.Add(roomType);

        await database.SaveChangesAsync(cancellationToken);

        return await RoomTypeRules.LoadAsync(database, roomType.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /my/boarding-houses/{id}/room-types/{typeId}. Raising the price is allowed and affects
/// nobody who already signed: a lease froze its own rent (docs/domain-rules.md §3.2).
/// </summary>
public sealed class UpdateRoomTypeHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<RoomTypeResponse> HandleAsync(
        Guid boardingHouseId,
        Guid roomTypeId,
        SaveRoomTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        RoomTypeRules.EnsureOccupancyFitsHouse(house, request.MaxOccupants);

        var roomType = await database.RoomTypes
            .Include(t => t.Facilities)
            .FirstOrDefaultAsync(
                t => t.Id == roomTypeId && t.BoardingHouseId == house.Id,
                cancellationToken)
            ?? throw new NotFoundException(MessageKeys.RoomType.NotFound);

        await EnsureCapCoversLiveTenantsAsync(roomType, request.MaxOccupants, cancellationToken);

        roomType.TypeName = request.TypeName.Trim();
        roomType.Price = request.Price;
        roomType.RoomSizeM2 = request.RoomSizeM2;
        roomType.MaxOccupants = request.MaxOccupants;
        roomType.Description = request.Description?.Trim();

        var facilities = await RoomTypeRules.LoadFacilitiesAsync(
            database, request.FacilityIds, cancellationToken);

        roomType.Facilities.Clear();

        foreach (var facility in facilities)
        {
            roomType.Facilities.Add(facility);
        }

        await database.SaveChangesAsync(cancellationToken);

        return await RoomTypeRules.LoadAsync(database, roomType.Id, cancellationToken);
    }

    /// <summary>
    /// Lowering the cap below what a room already holds would put existing rows in breach of
    /// §9.2 with nothing to fix them, so the tenants have to move out first.
    /// </summary>
    private async Task EnsureCapCoversLiveTenantsAsync(
        RoomType roomType,
        int requestedMaxOccupants,
        CancellationToken cancellationToken)
    {
        if (requestedMaxOccupants >= roomType.MaxOccupants)
        {
            return;
        }

        var liveCounts = await database.Leases
            .Where(l => l.Room.RoomTypeId == roomType.Id
                        && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring))
            .Select(l => l.Tenants.Count(t => t.MovedOutAt == null))
            .ToListAsync(cancellationToken);

        var occupied = liveCounts.Count == 0 ? 0 : liveCounts.Max();

        if (requestedMaxOccupants < occupied)
        {
            throw new BusinessRuleException(
                MessageKeys.RoomType.MaxOccupantsBelowLive, occupied);
        }
    }
}

/// <summary>
/// DELETE /my/boarding-houses/{id}/room-types/{typeId}. Soft delete, refused while a room still
/// points at it: a room without a type has no price and no occupancy cap.
/// </summary>
public sealed class DeleteRoomTypeHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid boardingHouseId,
        Guid roomTypeId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        var roomType = await database.RoomTypes.FirstOrDefaultAsync(
            t => t.Id == roomTypeId && t.BoardingHouseId == house.Id,
            cancellationToken)
            ?? throw new NotFoundException(MessageKeys.RoomType.NotFound);

        if (await database.Rooms.AnyAsync(r => r.RoomTypeId == roomType.Id, cancellationToken))
        {
            throw new ConflictException(MessageKeys.RoomType.InUse);
        }

        roomType.IsDeleted = true;

        await database.SaveChangesAsync(cancellationToken);
    }
}
