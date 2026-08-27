namespace MotelLease.Domain.Enums;

/// <summary>
/// Occupancy rules branch on these values (see docs/domain-rules.md §1), so they are an enum
/// rather than an admin-managed lookup table: a new row would have no rule attached to it.
/// </summary>
public enum BoardingHouseType
{
    /// <summary>One tenant per room.</summary>
    Traditional,

    /// <summary>One tenant per room.</summary>
    MiniHouse,

    /// <summary>Up to RoomType.MaxOccupants tenants per room.</summary>
    DormStyle
}

public enum ListingStatus
{
    Draft,
    PendingReview,
    Published,
    Rejected
}

/// <summary>
/// Four states rather than a boolean, because "deposited but not moved in yet" has to be
/// distinguishable: a room held by a paid deposit is neither free to book nor occupied.
/// </summary>
public enum RoomStatus
{
    Available,
    Reserved,
    Occupied,
    Maintenance
}

public enum UserRole
{
    Tenant,
    Staff,
    Owner,
    Admin
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum BusinessType
{
    Individual,
    Company
}
