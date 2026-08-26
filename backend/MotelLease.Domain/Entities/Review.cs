using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

public class Review : Entity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;

    /// <summary>Not null marks a verified review — the author actually rented here (§9.10).</summary>
    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }

    /// <summary>Set when this row is the owner's reply to another review.</summary>
    public Guid? ParentReviewId { get; set; }
    public Review? ParentReview { get; set; }

    public string Content { get; set; } = null!;

    /// <summary>1..5 for a review, null for a reply.</summary>
    public short? Rating { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<Review> Replies { get; set; } = [];
}

public class Report : Entity, ISoftDeletable
{
    public Guid ReporterUserId { get; set; }
    public User ReporterUser { get; set; } = null!;

    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    public string Reason { get; set; } = null!;
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public Guid? ProcessedByUserId { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Resolution { get; set; }

    public bool IsDeleted { get; set; }
}
