using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

/// <summary>
/// Polymorphic attachment. There is no FK: the target table is decided by
/// <see cref="OwnerType"/>, which keeps one upload pipeline instead of six tables.
/// Deleting a parent must delete its images explicitly.
/// </summary>
public class Image : Entity
{
    public ImageOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }

    public string Url { get; set; } = null!;

    /// <summary>Cloudinary public id, needed to remove the remote file.</summary>
    public string PublicId { get; set; } = null!;

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class SavedListing : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;
}
