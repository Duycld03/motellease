using Microsoft.EntityFrameworkCore;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Domain.Rooms;

namespace MotelLease.Application.BoardingHouses;

/// <summary>
/// GET /my/boarding-houses. One endpoint for both roles: an owner sees the properties they own,
/// a staff member the ones they hold a live assignment for (docs/api-design.md).
/// </summary>
public sealed class ListMyBoardingHousesHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<PagedResponse<BoardingHouseSummaryResponse>> HandleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await Paged.FromAsync(
            access.Managed()
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BoardingHouseSummaryResponse(
                    b.Id,
                    b.Name,
                    b.Type,
                    b.AddressLine,
                    b.Ward,
                    b.District,
                    b.Province,
                    b.ListingStatus,
                    b.Rating,
                    b.ReviewCount,
                    b.Rooms.Count,
                    b.Rooms.Count(r => r.Status == RoomStatus.Available),
                    b.RoomTypes.Min(t => (decimal?)t.Price),
                    b.RoomTypes.Max(t => (decimal?)t.Price),
                    database.Images
                        .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse
                                    && i.OwnerId == b.Id
                                    && i.IsPrimary)
                        .Select(i => i.Url)
                        .FirstOrDefault(),
                    b.CreatedAt)),
            page,
            pageSize,
            cancellationToken);
    }
}

/// <summary>
/// The detail projection, in one place: the read, the create and the update all answer with it,
/// and three copies would drift.
/// </summary>
internal static class BoardingHouseDetails
{
    internal static async Task<BoardingHouseDetailResponse> LoadAsync(
        IAppDbContext database,
        Guid id,
        CancellationToken cancellationToken) =>
        await database.BoardingHouses
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BoardingHouseDetailResponse(
                b.Id,
                b.OwnerUserId,
                b.Name,
                b.Description,
                b.Type,
                b.AddressLine,
                b.Ward,
                b.District,
                b.Province,
                b.Latitude,
                b.Longitude,
                b.ElectricityUnitPrice,
                b.WaterUnitPrice,
                b.ListingStatus,
                b.RejectionReason,
                b.Rating,
                b.ReviewCount,
                new RoomCountsResponse(
                    b.Rooms.Count,
                    b.Rooms.Count(r => r.Status == RoomStatus.Available),
                    b.Rooms.Count(r => r.Status == RoomStatus.Reserved),
                    b.Rooms.Count(r => r.Status == RoomStatus.Occupied),
                    b.Rooms.Count(r => r.Status == RoomStatus.Maintenance)),
                database.Images
                    .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == b.Id)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder))
                    .ToList(),
                b.CreatedAt,
                b.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);
}

/// <summary>GET /my/boarding-houses/{id}.</summary>
public sealed class GetBoardingHouseHandler(IAppDbContext database, BoardingHouseAccess access)
{
    public async Task<BoardingHouseDetailResponse> HandleAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        return await BoardingHouseDetails.LoadAsync(database, house.Id, cancellationToken);
    }
}

/// <summary>
/// POST /my/boarding-houses. The listing starts as a Draft: it becomes visible only after an
/// admin approves it (docs/features.md §3, P2). Latitude and Longitude are written; Location is
/// a generated column and PostgreSQL rejects any write to it (CLAUDE.md, Database rules).
/// </summary>
public sealed class CreateBoardingHouseHandler(IAppDbContext database, ICurrentUser currentUser)
{
    public async Task<BoardingHouseDetailResponse> HandleAsync(
        SaveBoardingHouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = new BoardingHouse
        {
            OwnerUserId = currentUser.RequireUserId(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            AddressLine = request.AddressLine.Trim(),
            Ward = request.Ward.Trim(),
            District = request.District.Trim(),
            Province = request.Province.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ListingStatus = ListingStatus.Draft
        };

        database.BoardingHouses.Add(house);

        await database.SaveChangesAsync(cancellationToken);

        return await BoardingHouseDetails.LoadAsync(database, house.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /my/boarding-houses/{id}. Assigned staff may edit the description and the address; the
/// listing status is not touched here — an admin owns that transition.
/// </summary>
public sealed class UpdateBoardingHouseHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<BoardingHouseDetailResponse> HandleAsync(
        Guid boardingHouseId,
        SaveBoardingHouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        // Occupancy branches on the house type (docs/domain-rules.md §1). Turning a shared house
        // into a single-occupant one while a room type still allows several tenants would leave
        // rows that break §9.2, so the room types have to be corrected first.
        if (house.Type != request.Type
            && !RoomOccupancyPolicy.AllowsSharing(request.Type)
            && await database.RoomTypes.AnyAsync(
                t => t.BoardingHouseId == house.Id && t.MaxOccupants > 1,
                cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.BoardingHouse.TypeConflictsWithOccupancy);
        }

        house.Name = request.Name.Trim();
        house.Description = request.Description?.Trim();
        house.Type = request.Type;
        house.AddressLine = request.AddressLine.Trim();
        house.Ward = request.Ward.Trim();
        house.District = request.District.Trim();
        house.Province = request.Province.Trim();
        house.Latitude = request.Latitude;
        house.Longitude = request.Longitude;

        await database.SaveChangesAsync(cancellationToken);

        return await BoardingHouseDetails.LoadAsync(database, house.Id, cancellationToken);
    }
}

/// <summary>
/// DELETE /my/boarding-houses/{id}. Soft delete, and only while nobody is living there or
/// holding a room with a paid deposit — the rows that record their money must keep pointing at
/// a property.
/// </summary>
public sealed class DeleteBoardingHouseHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireOwnerAsync(boardingHouseId, cancellationToken);

        var leased = await database.Leases.AnyAsync(
            l => l.Room.BoardingHouseId == house.Id
                 && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Expiring),
            cancellationToken);

        var held = await database.Deposits.AnyAsync(
            d => d.Room.BoardingHouseId == house.Id
                 && (d.Status == DepositStatus.Accepted || d.Status == DepositStatus.Paid),
            cancellationToken);

        if (leased || held)
        {
            throw new ConflictException(MessageKeys.BoardingHouse.InUse);
        }

        // The query filter is per entity and does not follow the parent, so the children are
        // marked in the same save. The images stay in storage: a soft delete is reversible
        // (docs/features.md §3, P2) and deleting the remote files would not be.
        house.IsDeleted = true;

        foreach (var room in await database.Rooms
                     .Where(r => r.BoardingHouseId == house.Id)
                     .ToListAsync(cancellationToken))
        {
            room.IsDeleted = true;
        }

        foreach (var roomType in await database.RoomTypes
                     .Where(t => t.BoardingHouseId == house.Id)
                     .ToListAsync(cancellationToken))
        {
            roomType.IsDeleted = true;
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// PUT /my/boarding-houses/{id}/submit-review. Draft or Rejected → PendingReview. A listing with
/// no rooms has nothing for an admin to review, so it is refused rather than queued.
/// </summary>
public sealed class SubmitBoardingHouseForReviewHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<BoardingHouseDetailResponse> HandleAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireOwnerAsync(boardingHouseId, cancellationToken);

        if (house.ListingStatus is not (ListingStatus.Draft or ListingStatus.Rejected))
        {
            throw new BusinessRuleException(MessageKeys.BoardingHouse.AlreadyUnderReview);
        }

        var hasRooms = await database.Rooms.AnyAsync(
            r => r.BoardingHouseId == house.Id, cancellationToken);

        if (!hasRooms)
        {
            throw new BusinessRuleException(MessageKeys.BoardingHouse.NothingToPublish);
        }

        house.ListingStatus = ListingStatus.PendingReview;

        // Cleared with the resubmission: the reason belongs to the rejection it explained, and
        // leaving it behind would make an approved listing look rejected.
        house.RejectionReason = null;

        await database.SaveChangesAsync(cancellationToken);

        return await BoardingHouseDetails.LoadAsync(database, house.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /my/boarding-houses/{id}/utility-prices. Bills multiply these by the metered quantity
/// (docs/domain-rules.md §3); an already issued bill froze its own amounts and does not change.
/// </summary>
public sealed class UpdateUtilityPricesHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<BoardingHouseDetailResponse> HandleAsync(
        Guid boardingHouseId,
        UpdateUtilityPricesRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireOwnerAsync(boardingHouseId, cancellationToken);

        house.ElectricityUnitPrice = request.ElectricityUnitPrice;
        house.WaterUnitPrice = request.WaterUnitPrice;

        await database.SaveChangesAsync(cancellationToken);

        return await BoardingHouseDetails.LoadAsync(database, house.Id, cancellationToken);
    }
}
