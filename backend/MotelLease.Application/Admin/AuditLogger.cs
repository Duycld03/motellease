using System.Text.Json;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Entities;

namespace MotelLease.Application.Admin;

public sealed class AuditLogger(IAppDbContext database, TimeProvider time)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Log<TBefore, TAfter>(
        Guid actorUserId,
        string action,
        string entityType,
        Guid? entityId,
        TBefore? before = default,
        TAfter? after = default,
        string? ipAddress = null)
    {
        var log = new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = before is not null ? JsonSerializer.Serialize(before, JsonOptions) : null,
            AfterJson = after is not null ? JsonSerializer.Serialize(after, JsonOptions) : null,
            IpAddress = ipAddress,
            CreatedAt = time.GetUtcNow()
        };

        database.AuditLogs.Add(log);
    }
}
