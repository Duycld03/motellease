using MotelLease.Domain.Common;

namespace MotelLease.Domain.Entities;

public class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Hash of the token, never the token itself — a leaked table stays unusable.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Set when this token was rotated, so a replayed old token exposes the chain.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}

public class StaffAssignment : Entity
{
    public Guid BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;

    public Guid StaffUserId { get; set; }
    public User StaffUser { get; set; } = null!;

    public Guid AssignedByUserId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    /// <summary>Null while the assignment is live. Drives staff authorization (§9.12).</summary>
    public DateTimeOffset? UnassignedAt { get; set; }
}
