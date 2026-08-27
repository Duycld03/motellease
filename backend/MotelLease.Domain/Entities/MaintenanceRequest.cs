using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

public class MaintenanceRequest : Entity
{
    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid ReportedByUserId { get; set; }
    public User ReportedByUser { get; set; } = null!;

    public MaintenanceCategory Category { get; set; }
    public string Description { get; set; } = null!;

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    /// <summary>
    /// The work item created from this request. The FK lives on WorkTask (the dependent —
    /// a request exists before anyone is assigned), so the pair cannot drift out of sync.
    /// </summary>
    public WorkTask? Task { get; set; }
}

/// <summary>
/// Named WorkTask, not Task, to avoid colliding with <see cref="System.Threading.Tasks.Task"/>.
/// Table name stays Tasks.
/// </summary>
public class WorkTask : Entity
{
    /// <summary>
    /// Added in this version: without it an owner cannot list the work of one house
    /// (docs/erd.md §5).
    /// </summary>
    public Guid BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public Guid AssignedToUserId { get; set; }
    public User AssignedToUser { get; set; } = null!;

    public Guid? MaintenanceRequestId { get; set; }
    public MaintenanceRequest? MaintenanceRequest { get; set; }

    public string Title { get; set; } = null!;
    public string? Details { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.InProgress;

    public DateOnly? DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
