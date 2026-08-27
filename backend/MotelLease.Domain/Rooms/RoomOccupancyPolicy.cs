using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Rooms;

/// <summary>
/// How many people may live in one room (docs/domain-rules.md §1). Only a DormStyle house
/// shares a room, so <c>RoomType.MaxOccupants</c> is the cap there and one tenant is the cap
/// everywhere else.
///
/// A rule that has to be tested against a database is in the wrong layer, which is why this is
/// a pure function taking the two values it needs rather than an EF interceptor.
/// </summary>
public static class RoomOccupancyPolicy
{
    public static bool AllowsSharing(BoardingHouseType houseType) =>
        houseType is BoardingHouseType.DormStyle;

    public static int MaxOccupants(BoardingHouseType houseType, int roomTypeMaxOccupants) =>
        AllowsSharing(houseType) ? roomTypeMaxOccupants : 1;
}
