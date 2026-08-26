using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MotelLease.Domain.Common;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence;

public class MotelLeaseDbContext(DbContextOptions<MotelLeaseDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OwnerProfile> OwnerProfiles => Set<OwnerProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BoardingHouse> BoardingHouses => Set<BoardingHouse>();
    public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<SavedListing> SavedListings => Set<SavedListing>();

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<LeaseTenant> LeaseTenants => Set<LeaseTenant>();
    public DbSet<ExtensionRequest> ExtensionRequests => Set<ExtensionRequest>();

    public DbSet<PaymentBill> PaymentBills => Set<PaymentBill>();
    public DbSet<RoomAdditionalFee> RoomAdditionalFees => Set<RoomAdditionalFee>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<WithdrawRequest> WithdrawRequests => Set<WithdrawRequest>();
    public DbSet<BoardingHouseExpense> BoardingHouseExpenses => Set<BoardingHouseExpense>();

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Soft-deletable principals (User, Room, BoardingHouse) are pointed at by rows that
        // are not soft-deletable themselves — a bill still belongs to a closed account. EF
        // warns that the required navigation can be filtered out; that is accepted here.
        // Any query that must show such a row uses IgnoreQueryFilters on the principal.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(
            CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PostGIS only. gen_random_uuid() needs no extension on PostgreSQL 13+, and ids are
        // generated as UUIDv7 by the application anyway — see docs/erd.md §8.
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MotelLeaseDbContext).Assembly);

        // Money defaults to decimal(18,2) everywhere unless a configuration overrides it.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // Enums are stored as text so adding a value never needs an ALTER TYPE migration.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties()))
        {
            var enumType = property.ClrType.IsEnum
                ? property.ClrType
                : Nullable.GetUnderlyingType(property.ClrType) is { IsEnum: true } inner
                    ? inner
                    : null;

            if (enumType is null)
            {
                continue;
            }

            property.SetValueConverter(
                (ValueConverter)Activator.CreateInstance(
                    typeof(EnumToStringConverter<>).MakeGenericType(enumType))!);
        }

        // Soft delete: every query excludes deleted rows. Unique indexes on these tables
        // are declared as partial indexes so a deleted row never blocks a new one.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(Expression.Not(isDeleted), parameter));
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(e => e.CreatedAt).IsModified = false;
            }
        }

        // AuditLog is append-only and has no UpdatedAt, so it is stamped separately.
        foreach (var entry in ChangeTracker.Entries<AuditLog>()
                     .Where(e => e.State == EntityState.Added))
        {
            entry.Entity.CreatedAt = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
