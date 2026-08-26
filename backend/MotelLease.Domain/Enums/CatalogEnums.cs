namespace MotelLease.Domain.Enums;

/// <summary>
/// Replaces the admin-managed BoardingHouseType table from the original project.
/// Occupancy rules branch on these values (see docs/domain-rules.md §1), so they must not
/// be editable at runtime.
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
/// Replaces the boolean isAvailable of the original project, which could not express
/// "deposited but not moved in yet" and therefore needed a nightly repair job.
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
