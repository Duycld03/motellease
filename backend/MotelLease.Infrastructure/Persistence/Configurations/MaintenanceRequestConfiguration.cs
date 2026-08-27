using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.Property(r => r.Description).HasMaxLength(2048).IsRequired();

        builder.HasIndex(r => new { r.RoomId, r.Status });
        builder.HasIndex(r => r.LeaseId);

        builder.HasOne(r => r.Lease)
            .WithMany()
            .HasForeignKey(r => r.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Room)
            .WithMany()
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReportedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("Tasks");

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Details).HasMaxLength(2048);

        builder.HasIndex(t => new { t.BoardingHouseId, t.Status });
        builder.HasIndex(t => new { t.AssignedToUserId, t.Status });
        builder.HasIndex(t => new { t.DueDate, t.Status });

        builder.HasOne(t => t.BoardingHouse)
            .WithMany()
            .HasForeignKey(t => t.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One task per maintenance request; the unique index is partial so unrelated
        // tasks (which have no request) are unaffected.
        builder.HasOne(t => t.MaintenanceRequest)
            .WithOne(r => r.Task)
            .HasForeignKey<WorkTask>(t => t.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.MaintenanceRequestId)
            .IsUnique()
            .HasFilter("\"MaintenanceRequestId\" IS NOT NULL");
    }
}
