namespace MotelLease.Domain.Common;

/// <summary>
/// Base for every persisted entity. CreatedAt/UpdatedAt are maintained by the
/// DbContext, not by callers.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Marks an entity that is hidden rather than removed. Applied through a global query
/// filter, so every query excludes deleted rows unless it opts out explicitly.
/// Unique indexes on these tables must be partial (WHERE "IsDeleted" = false).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
