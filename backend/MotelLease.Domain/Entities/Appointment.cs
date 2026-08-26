using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

public class Appointment : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTimeOffset AppointmentDate { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public string? Note { get; set; }
    public string? ReasonForCancel { get; set; }

    /// <summary>Owner or assigned staff who accepted or rejected the visit.</summary>
    public Guid? HandledByUserId { get; set; }
}

public class Deposit : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    /// <summary>
    /// One month of rent, frozen when the request is made. Reading RoomType.Price later
    /// would change the amount owed after the fact (docs/domain-rules.md §2).
    /// </summary>
    public decimal Amount { get; set; }

    public DepositStatus Status { get; set; } = DepositStatus.Pending;

    public DateOnly RequestedStartDate { get; set; }
    public int RequestedTermMonths { get; set; }

    /// <summary>
    /// Payment deadline, set when the owner accepts. Past it the deposit expires and the
    /// room returns to Available — the original project held rooms indefinitely.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    public string? ReasonForCancel { get; set; }
    public Guid? HandledByUserId { get; set; }

    public Lease? Lease { get; set; }
}
