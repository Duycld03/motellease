using MotelLease.Domain.Enums;

namespace MotelLease.Application.Rooms.Contracts;

public sealed record RoomResponse(
    Guid Id,
    Guid BoardingHouseId,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal Price,
    string RoomNumber,
    RoomStatus Status,
    string? Description,
    decimal CurrentElectricityReading,
    decimal CurrentWaterReading,
    DateTimeOffset UpdatedAt);

public sealed record SaveRoomRequest(Guid RoomTypeId, string RoomNumber, string? Description);

/// <summary>Only Available and Maintenance are reachable here — see RoomStatusPolicy.</summary>
public sealed record UpdateRoomStatusRequest(RoomStatus Status);

public sealed record UpdateMeterReadingsRequest(
    decimal ElectricityReading,
    decimal WaterReading);
