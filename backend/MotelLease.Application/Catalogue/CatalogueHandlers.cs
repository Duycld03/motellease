using Microsoft.EntityFrameworkCore;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Catalogue.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.RoomTypes.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using NetTopologySuite.Geometries;

namespace MotelLease.Application.Catalogue;

public sealed class SearchBoardingHousesHandler(IAppDbContext database)
{
    public async Task<PagedResponse<PublicBoardingHouseCardResponse>> HandleAsync(
        BoardingHouseSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = database.BoardingHouses
            .AsNoTracking()
            .Where(b => b.ListingStatus == ListingStatus.Published);

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim().ToLower();
            query = query.Where(b =>
                b.Name.ToLower().Contains(q) ||
                b.AddressLine.ToLower().Contains(q) ||
                (b.Description != null && b.Description.ToLower().Contains(q)) ||
                b.Ward.ToLower().Contains(q) ||
                b.District.ToLower().Contains(q) ||
                b.Province.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            var province = filter.Province.Trim();
            query = query.Where(b => b.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            var district = filter.District.Trim();
            query = query.Where(b => b.District == district);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(b => b.Type == filter.Type.Value);
        }

        if (filter.MinRating.HasValue)
        {
            query = query.Where(b => b.Rating >= filter.MinRating.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(b => b.RoomTypes.Any(rt => rt.Price >= filter.MinPrice.Value));
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(b => b.RoomTypes.Any(rt => rt.Price <= filter.MaxPrice.Value));
        }

        if (filter.Facilities is { Count: > 0 } facIds)
        {
            foreach (var facId in facIds)
            {
                query = query.Where(b => b.RoomTypes.Any(rt => rt.Facilities.Any(f => f.Id == facId)));
            }
        }

        query = filter.Sort?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(b => b.RoomTypes.Min(rt => (decimal?)rt.Price)),
            "price_desc" => query.OrderByDescending(b => b.RoomTypes.Max(rt => (decimal?)rt.Price)),
            "rating_desc" => query.OrderByDescending(b => b.Rating).ThenByDescending(b => b.ReviewCount),
            _ => query.OrderByDescending(b => b.CreatedAt)
        };

        var projected = ProjectCards(query, database);
        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }

    internal static IQueryable<PublicBoardingHouseCardResponse> ProjectCards(
        IQueryable<BoardingHouse> query,
        IAppDbContext database) =>
        query.Select(b => new PublicBoardingHouseCardResponse(
            b.Id,
            b.Name,
            b.Type,
            b.AddressLine,
            b.Ward,
            b.District,
            b.Province,
            b.Latitude,
            b.Longitude,
            b.Rating,
            b.ReviewCount,
            b.RoomTypes.Min(rt => (decimal?)rt.Price),
            b.RoomTypes.Max(rt => (decimal?)rt.Price),
            database.Images
                .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == b.Id && i.IsPrimary)
                .Select(i => i.Url)
                .FirstOrDefault(),
            b.Rooms.Count(),
            b.Rooms.Count(r => r.Status == RoomStatus.Available),
            b.RoomTypes
                .SelectMany(rt => rt.Facilities)
                .Distinct()
                .Select(f => new FacilityResponse(f.Id, f.Name, f.CodeName, f.IconKey))
                .ToList(),
            b.CreatedAt));
}

public sealed class GetNearbyBoardingHousesHandler(IAppDbContext database)
{
    public async Task<IReadOnlyList<BoardingHouseNearbyResponse>> HandleAsync(
        BoardingHouseNearbyRequest request,
        CancellationToken cancellationToken = default)
    {
        // Longitude first in Point constructor (docs/seed-plan.md §3, domain-rules.md §9)
        var userPoint = new Point(request.Lon, request.Lat) { SRID = 4326 };
        var radiusMeters = request.RadiusKm * 1000.0;
        var limit = Math.Clamp(request.Limit, 1, 100);

        var rawList = await database.BoardingHouses
            .AsNoTracking()
            .Where(b => b.ListingStatus == ListingStatus.Published &&
                        EF.Property<Point>(b, "Location").IsWithinDistance(userPoint, radiusMeters))
            .OrderBy(b => EF.Property<Point>(b, "Location").Distance(userPoint))
            .Take(limit)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Type,
                b.AddressLine,
                b.Ward,
                b.District,
                b.Province,
                b.Latitude,
                b.Longitude,
                b.Rating,
                b.ReviewCount,
                MinPrice = b.RoomTypes.Min(rt => (decimal?)rt.Price),
                MaxPrice = b.RoomTypes.Max(rt => (decimal?)rt.Price),
                PrimaryImageUrl = database.Images
                    .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == b.Id && i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                TotalRoomsCount = b.Rooms.Count(),
                AvailableRoomsCount = b.Rooms.Count(r => r.Status == RoomStatus.Available),
                Facilities = b.RoomTypes
                    .SelectMany(rt => rt.Facilities)
                    .Distinct()
                    .Select(f => new FacilityResponse(f.Id, f.Name, f.CodeName, f.IconKey))
                    .ToList(),
                DistanceMeters = EF.Property<Point>(b, "Location").Distance(userPoint),
                b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return rawList.Select(b => new BoardingHouseNearbyResponse(
            b.Id,
            b.Name,
            b.Type,
            b.AddressLine,
            b.Ward,
            b.District,
            b.Province,
            b.Latitude,
            b.Longitude,
            b.Rating,
            b.ReviewCount,
            b.MinPrice,
            b.MaxPrice,
            b.PrimaryImageUrl,
            b.TotalRoomsCount,
            b.AvailableRoomsCount,
            b.Facilities,
            b.DistanceMeters,
            b.CreatedAt)).ToList();
    }
}

public sealed class GetMapBoardingHousesHandler(IAppDbContext database)
{
    public async Task<IReadOnlyList<BoardingHouseMapMarkerResponse>> HandleAsync(
        BoardingHouseMapRequest request,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(request.Limit, 1, 300);
        var minLat = (decimal)Math.Min(request.SwLat, request.NeLat);
        var maxLat = (decimal)Math.Max(request.SwLat, request.NeLat);
        var minLon = (decimal)Math.Min(request.SwLon, request.NeLon);
        var maxLon = (decimal)Math.Max(request.SwLon, request.NeLon);

        return await database.BoardingHouses
            .AsNoTracking()
            .Where(b => b.ListingStatus == ListingStatus.Published &&
                        b.Latitude >= minLat && b.Latitude <= maxLat &&
                        b.Longitude >= minLon && b.Longitude <= maxLon)
            .Take(limit)
            .Select(b => new BoardingHouseMapMarkerResponse(
                b.Id,
                b.Name,
                b.Latitude,
                b.Longitude,
                b.RoomTypes.Min(rt => (decimal?)rt.Price),
                b.RoomTypes.Max(rt => (decimal?)rt.Price),
                database.Images
                    .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == b.Id && i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                b.AddressLine,
                b.Rating,
                b.ReviewCount))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetBoardingHouseDetailHandler(IAppDbContext database)
{
    public async Task<PublicBoardingHouseDetailResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var house = await database.BoardingHouses
            .AsNoTracking()
            .Include(b => b.OwnerUser)
            .Include(b => b.RoomTypes)
                .ThenInclude(rt => rt.Facilities)
            .Include(b => b.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id && b.ListingStatus == ListingStatus.Published, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

        var houseImages = await database.Images
            .AsNoTracking()
            .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == id)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder))
            .ToListAsync(cancellationToken);

        var roomTypeIds = house.RoomTypes.Select(rt => rt.Id).ToList();
        var roomTypeImages = await database.Images
            .AsNoTracking()
            .Where(i => i.OwnerType == ImageOwnerType.RoomType && roomTypeIds.Contains(i.OwnerId))
            .OrderBy(i => i.SortOrder)
            .Select(i => new { i.OwnerId, Image = new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder) })
            .ToListAsync(cancellationToken);

        var roomTypeImagesMap = roomTypeImages
            .GroupBy(x => x.OwnerId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Image).ToList());

        var roomTypes = house.RoomTypes.Select(rt => new PublicRoomTypeResponse(
            rt.Id,
            rt.TypeName,
            rt.Price,
            rt.RoomSizeM2,
            rt.MaxOccupants,
            rt.Description,
            house.Rooms.Count(r => r.RoomTypeId == rt.Id),
            house.Rooms.Count(r => r.RoomTypeId == rt.Id && r.Status == RoomStatus.Available),
            rt.Facilities.Select(f => new FacilityResponse(f.Id, f.Name, f.CodeName, f.IconKey)).ToList(),
            roomTypeImagesMap.GetValueOrDefault(rt.Id, []))).ToList();

        var owner = new PublicOwnerInfoResponse(
            house.OwnerUserId,
            house.OwnerUser.FullName,
            house.OwnerUser.PhoneNumber,
            house.OwnerUser.AvatarUrl);

        return new PublicBoardingHouseDetailResponse(
            house.Id,
            house.Name,
            house.Description,
            house.Type,
            house.AddressLine,
            house.Ward,
            house.District,
            house.Province,
            house.Latitude,
            house.Longitude,
            house.ElectricityUnitPrice,
            house.WaterUnitPrice,
            house.Rating,
            house.ReviewCount,
            house.Rooms.Count,
            house.Rooms.Count(r => r.Status == RoomStatus.Available),
            owner,
            houseImages,
            roomTypes,
            house.CreatedAt);
    }
}

public sealed class GetBoardingHouseVacantRoomsHandler(IAppDbContext database)
{
    public async Task<IReadOnlyList<PublicVacantRoomResponse>> HandleAsync(
        Guid houseId,
        CancellationToken cancellationToken = default)
    {
        return await database.Rooms
            .AsNoTracking()
            .Where(r => r.BoardingHouseId == houseId &&
                        r.Status == RoomStatus.Available &&
                        r.BoardingHouse.ListingStatus == ListingStatus.Published)
            .OrderBy(r => r.RoomNumber)
            .Select(r => new PublicVacantRoomResponse(
                r.Id,
                r.RoomNumber,
                r.RoomTypeId,
                r.RoomType.TypeName,
                r.RoomType.Price,
                r.RoomType.RoomSizeM2,
                r.RoomType.MaxOccupants,
                r.Description))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetBoardingHouseReviewsHandler(IAppDbContext database)
{
    public async Task<PagedResponse<PublicReviewResponse>> HandleAsync(
        Guid houseId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = database.Reviews
            .AsNoTracking()
            .Where(r => r.BoardingHouseId == houseId && r.ParentReviewId == null)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PublicReviewResponse(
                r.Id,
                r.UserId,
                r.User.FullName,
                r.User.AvatarUrl,
                r.Rating ?? 5,
                r.Content,
                r.LeaseId != null,
                r.CreatedAt,
                r.Replies
                    .OrderBy(rep => rep.CreatedAt)
                    .Select(rep => new PublicReviewReplyResponse(
                        rep.Id,
                        rep.UserId,
                        rep.User.FullName,
                        rep.User.AvatarUrl,
                        rep.Content,
                        rep.CreatedAt))
                    .ToList()));

        return await Paged.FromAsync(query, page, pageSize, cancellationToken);
    }
}

public sealed class ListFacilitiesHandler(IAppDbContext database)
{
    public async Task<IReadOnlyList<FacilityResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        return await database.Facilities
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new FacilityResponse(f.Id, f.Name, f.CodeName, f.IconKey))
            .ToListAsync(cancellationToken);
    }
}

public sealed class ListProvincesHandler(IAppDbContext database)
{
    public async Task<IReadOnlyList<ProvinceResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        // Query distinct provinces from database and fallback to default regions if empty
        var dbProvinces = await database.BoardingHouses
            .AsNoTracking()
            .Select(b => b.Province)
            .Distinct()
            .ToListAsync(cancellationToken);

        var standardProvinces = new List<ProvinceResponse>
        {
            new("HN", "Hà Nội"),
            new("HCM", "Hồ Chí Minh"),
            new("DN", "Đà Nẵng"),
            new("BD", "Bình Dương"),
            new("DNA", "Đồng Nai"),
            new("CT", "Cần Thơ"),
            new("HP", "Hải Phòng"),
            new("QN", "Quảng Ninh"),
            new("HUE", "Thừa Thiên Huế"),
            new("KH", "Khánh Hòa")
        };

        foreach (var p in dbProvinces.Where(p => standardProvinces.All(sp => sp.Name != p)))
        {
            standardProvinces.Add(new(p.ToUpperInvariant(), p));
        }

        return standardProvinces;
    }
}

public sealed class ListDistrictsHandler(IAppDbContext database)
{
    public async Task<IReadOnlyList<DistrictResponse>> HandleAsync(
        string provinceCode,
        CancellationToken cancellationToken = default)
    {
        var provinceName = provinceCode.ToUpperInvariant() switch
        {
            "HN" => "Hà Nội",
            "HCM" => "Hồ Chí Minh",
            "DN" => "Đà Nẵng",
            "BD" => "Bình Dương",
            "DNA" => "Đồng Nai",
            "CT" => "Cần Thơ",
            "HP" => "Hải Phòng",
            _ => provinceCode
        };

        var dbDistricts = await database.BoardingHouses
            .AsNoTracking()
            .Where(b => b.Province == provinceName || b.Province == provinceCode)
            .Select(b => b.District)
            .Distinct()
            .ToListAsync(cancellationToken);

        var defaults = provinceCode.ToUpperInvariant() switch
        {
            "HN" => new List<string> { "Cầu Giấy", "Đống Đa", "Hai Bà Trưng", "Thanh Xuân", "Ba Đình", "Hoàng Mai", "Hà Đông", "Nam Từ Liêm", "Bắc Từ Liêm", "Tây Hồ" },
            "HCM" => new List<string> { "Quận 1", "Quận 3", "Quận 5", "Quận 7", "Quận 10", "Tân Bình", "Bình Thạnh", "Gò Vấp", "Phú Nhuận", "Thủ Đức" },
            _ => new List<string>()
        };

        var result = new List<DistrictResponse>();
        var allNames = defaults.Union(dbDistricts).Distinct().ToList();
        for (var i = 0; i < allNames.Count; i++)
        {
            result.Add(new($"D{i + 1}", allNames[i], provinceCode));
        }

        return result;
    }
}
