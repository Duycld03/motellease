using Microsoft.EntityFrameworkCore;
using MotelLease.Domain.Entities;

namespace MotelLease.Application.Common.Abstractions;

/// <summary>
/// The database as the use-case handlers see it. Implemented by MotelLeaseDbContext, so
/// handlers stay in Application while the mapping stays in Infrastructure.
/// Only the sets a shipped feature needs are exposed; the list grows per feature.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<OwnerProfile> OwnerProfiles { get; }
    DbSet<StaffProfile> StaffProfiles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<StaffAssignment> StaffAssignments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<BoardingHouse> BoardingHouses { get; }
    DbSet<Facility> Facilities { get; }
    DbSet<RoomType> RoomTypes { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Image> Images { get; }

    DbSet<Deposit> Deposits { get; }
    DbSet<Lease> Leases { get; }

    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<RefundRequest> RefundRequests { get; }

    DbSet<Appointment> Appointments { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// One transaction around a whole use case. Required by every flow that moves money
    /// (docs/domain-rules.md §9) and used here to keep refresh-token rotation atomic.
    /// </summary>
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
