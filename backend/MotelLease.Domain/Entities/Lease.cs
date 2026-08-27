using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

public class Lease : Entity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    /// <summary>Null for a lease signed without an online deposit.</summary>
    public Guid? DepositId { get; set; }
    public Deposit? Deposit { get; set; }

    public Guid PrimaryTenantUserId { get; set; }
    public User PrimaryTenant { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TermMonths { get; set; }

    /// <summary>
    /// Rent agreed at signing. Bills read this, never RoomType.Price, so a later price
    /// change cannot rewrite an existing contract (docs/domain-rules.md §3).
    /// </summary>
    public decimal MonthlyRent { get; set; }

    /// <summary>Deposit money held for the duration of the lease.</summary>
    public decimal DepositHeld { get; set; }

    public LeaseStatus Status { get; set; } = LeaseStatus.Active;

    public DateTimeOffset? EndedAt { get; set; }
    public string? EndReason { get; set; }

    /// <summary>Meter readings at handover, used to settle the final bill.</summary>
    public decimal? FinalElectricityReading { get; set; }
    public decimal? FinalWaterReading { get; set; }

    public decimal DepositDeducted { get; set; }
    public decimal DepositRefunded { get; set; }

    public Guid CreatedByUserId { get; set; }

    public ICollection<LeaseTenant> Tenants { get; set; } = [];
    public ICollection<PaymentBill> Bills { get; set; } = [];
}

/// <summary>
/// A person living in the room. UserId is nullable because a co-tenant does not need an
/// account; occupancy is counted from the rows where MovedOutAt is null (§9.2).
/// </summary>
public class LeaseTenant : Entity
{
    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? IdCardNumber { get; set; }

    public bool IsPrimary { get; set; }

    public DateTimeOffset MovedInAt { get; set; }
    public DateTimeOffset? MovedOutAt { get; set; }
}

public class ExtensionRequest : Entity
{
    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    /// <summary>Copied at request time so the owner sees what was asked, not today's value.</summary>
    public DateOnly CurrentEndDate { get; set; }
    public DateOnly RequestedEndDate { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public string? TenantNote { get; set; }
    public string? OwnerNote { get; set; }
    public Guid? HandledByUserId { get; set; }
}
