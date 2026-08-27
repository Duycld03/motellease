namespace MotelLease.Application.RoomTypes.Contracts;

public sealed record RoomTypeResponse(
    Guid Id,
    Guid BoardingHouseId,
    string TypeName,
    decimal Price,
    decimal RoomSizeM2,
    int MaxOccupants,
    string? Description,
    int RoomCount,
    IReadOnlyList<FacilityResponse> Facilities);

public sealed record FacilityResponse(Guid Id, string Name, string CodeName, string? IconKey);

/// <summary>
/// Create and update take the same shape: a room type is small enough that a partial update
/// would only trade one round trip for the ambiguity of "absent means unchanged".
/// <paramref name="FacilityIds"/> replaces the whole set.
/// </summary>
public sealed record SaveRoomTypeRequest(
    string TypeName,
    decimal Price,
    decimal RoomSizeM2,
    int MaxOccupants,
    string? Description,
    IReadOnlyList<Guid> FacilityIds);
