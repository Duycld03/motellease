using Microsoft.EntityFrameworkCore;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Catalogue;
using MotelLease.Application.Catalogue.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Application.SavedListings.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.SavedListings;

public sealed class ListSavedListingsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<SavedListingResponse>> HandleAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var userId = currentUser.RequireUserId();

        var query = database.SavedListings
            .AsNoTracking()
            .Where(sl => sl.UserId == userId &&
                         sl.BoardingHouse.ListingStatus == ListingStatus.Published)
            .OrderByDescending(sl => sl.CreatedAt)
            .Select(sl => new SavedListingResponse(
                sl.Id,
                sl.BoardingHouseId,
                new PublicBoardingHouseCardResponse(
                    sl.BoardingHouse.Id,
                    sl.BoardingHouse.Name,
                    sl.BoardingHouse.Type,
                    sl.BoardingHouse.AddressLine,
                    sl.BoardingHouse.Ward,
                    sl.BoardingHouse.District,
                    sl.BoardingHouse.Province,
                    sl.BoardingHouse.Latitude,
                    sl.BoardingHouse.Longitude,
                    sl.BoardingHouse.Rating,
                    sl.BoardingHouse.ReviewCount,
                    sl.BoardingHouse.RoomTypes.Min(rt => (decimal?)rt.Price),
                    sl.BoardingHouse.RoomTypes.Max(rt => (decimal?)rt.Price),
                    database.Images
                        .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == sl.BoardingHouseId && i.IsPrimary)
                        .Select(i => i.Url)
                        .FirstOrDefault(),
                    sl.BoardingHouse.Rooms.Count(),
                    sl.BoardingHouse.Rooms.Count(r => r.Status == RoomStatus.Available),
                    sl.BoardingHouse.RoomTypes
                        .SelectMany(rt => rt.Facilities)
                        .Distinct()
                        .Select(f => new FacilityResponse(f.Id, f.Name, f.CodeName, f.IconKey))
                        .ToList(),
                    sl.BoardingHouse.CreatedAt),
                sl.CreatedAt));

        return await Paged.FromAsync(query, page, pageSize, cancellationToken);
    }
}

public sealed class SaveListingHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<SavedListingResponse> HandleAsync(
        SaveListingRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var house = await database.BoardingHouses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardingHouseId &&
                                      b.ListingStatus == ListingStatus.Published,
                cancellationToken)
            ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

        var existing = await database.SavedListings
            .FirstOrDefaultAsync(sl => sl.UserId == userId &&
                                      sl.BoardingHouseId == request.BoardingHouseId,
                cancellationToken);

        if (existing is null)
        {
            existing = new SavedListing
            {
                UserId = userId,
                BoardingHouseId = request.BoardingHouseId
            };
            database.SavedListings.Add(existing);
            await database.SaveChangesAsync(cancellationToken);
        }

        var card = await SearchBoardingHousesHandler
            .ProjectCards(database.BoardingHouses.AsNoTracking().Where(b => b.Id == request.BoardingHouseId), database)
            .FirstAsync(cancellationToken);

        return new SavedListingResponse(existing.Id, existing.BoardingHouseId, card, existing.CreatedAt);
    }
}

public sealed class RemoveSavedListingHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var existing = await database.SavedListings
            .FirstOrDefaultAsync(sl => sl.UserId == userId &&
                                      sl.BoardingHouseId == boardingHouseId,
                cancellationToken);

        if (existing is not null)
        {
            database.SavedListings.Remove(existing);
            await database.SaveChangesAsync(cancellationToken);
        }
    }
}
