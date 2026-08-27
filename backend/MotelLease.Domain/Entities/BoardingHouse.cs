using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

public class BoardingHouse : Entity, ISoftDeletable
{
    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public BoardingHouseType Type { get; set; }

    public string AddressLine { get; set; } = null!;
    public string Ward { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Province { get; set; } = null!;

    /// <summary>
    /// Latitude/Longitude are the writable source of truth. Location is a STORED generated
    /// column derived from them — never assign to it (PostgreSQL rejects the write).
    /// Keeping lat/lon as the input also means PostGIS can be dropped later without a
    /// data migration. See docs/erd.md §8.
    /// </summary>
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public decimal ElectricityUnitPrice { get; set; }
    public decimal WaterUnitPrice { get; set; }

    public ListingStatus ListingStatus { get; set; } = ListingStatus.Draft;
    public string? RejectionReason { get; set; }

    /// <summary>Cached average rating, recomputed when reviews change.</summary>
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<RoomType> RoomTypes { get; set; } = [];
    public ICollection<Room> Rooms { get; set; } = [];
}

public class Facility : Entity, ISoftDeletable
{
    public string Name { get; set; } = null!;

    /// <summary>Stable key used by the frontend to pick an icon and a translation.</summary>
    public string CodeName { get; set; } = null!;

    public string? Description { get; set; }
    public string? IconKey { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<RoomType> RoomTypes { get; set; } = [];
}

public class RoomType : Entity, ISoftDeletable
{
    public Guid BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    /// <summary>Current asking price. A signed lease freezes its own copy in Lease.MonthlyRent.</summary>
    public decimal Price { get; set; }

    public decimal RoomSizeM2 { get; set; }

    /// <summary>Occupancy cap for DormStyle houses (docs/domain-rules.md §1).</summary>
    public int MaxOccupants { get; set; } = 1;

    public string? Description { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<Facility> Facilities { get; set; } = [];
    public ICollection<Room> Rooms { get; set; } = [];
}

public class Room : Entity, ISoftDeletable
{
    public Guid BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;

    public Guid RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string? Description { get; set; }

    /// <summary>
    /// Running meter reading. The previous reading is not duplicated here — it is
    /// PaymentBill.ElectricityNew of the prior month, so there is one source of truth.
    /// </summary>
    public decimal CurrentElectricityReading { get; set; }
    public decimal CurrentWaterReading { get; set; }

    public bool IsDeleted { get; set; }
}
