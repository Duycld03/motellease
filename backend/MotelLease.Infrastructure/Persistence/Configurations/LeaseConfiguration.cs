using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.Property(l => l.EndReason).HasMaxLength(512);
        builder.Property(l => l.FinalElectricityReading).HasColumnType("decimal(12,2)");
        builder.Property(l => l.FinalWaterReading).HasColumnType("decimal(12,2)");

        // Verified on PostGIS image: blocks a second Active lease on a room while still
        // allowing many Ended ones (docs/erd.md §8.5, invariant §9.1).
        builder.HasIndex(l => l.RoomId, "IX_Leases_RoomId_Active")
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        builder.HasIndex(l => new { l.PrimaryTenantUserId, l.Status });
        builder.HasIndex(l => new { l.EndDate, l.Status });

        builder.HasOne(l => l.Room)
            .WithMany()
            .HasForeignKey(l => l.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Deposit)
            .WithOne(d => d.Lease)
            .HasForeignKey<Lease>(l => l.DepositId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.PrimaryTenant)
            .WithMany()
            .HasForeignKey(l => l.PrimaryTenantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Leases_Dates_Ordered", "\"EndDate\" > \"StartDate\"");
            t.HasCheckConstraint("CK_Leases_TermMonths_Positive", "\"TermMonths\" >= 1");
            t.HasCheckConstraint("CK_Leases_MonthlyRent_NonNegative", "\"MonthlyRent\" >= 0");
            // The held deposit can be deducted from and refunded, never over-consumed.
            t.HasCheckConstraint(
                "CK_Leases_DepositSettlement",
                "\"DepositDeducted\" + \"DepositRefunded\" <= \"DepositHeld\"");
        });
    }
}

public class LeaseTenantConfiguration : IEntityTypeConfiguration<LeaseTenant>
{
    public void Configure(EntityTypeBuilder<LeaseTenant> builder)
    {
        builder.Property(t => t.FullName).HasMaxLength(128).IsRequired();
        builder.Property(t => t.PhoneNumber).HasMaxLength(20);
        builder.Property(t => t.IdCardNumber).HasMaxLength(32);

        builder.HasIndex(t => t.LeaseId);

        // Occupancy counts come off this index (invariant §9.2). The named overload keeps
        // it separate from the plain LeaseId index above.
        builder.HasIndex(t => t.LeaseId, "IX_LeaseTenants_LeaseId_Living")
            .HasFilter("\"MovedOutAt\" IS NULL");

        // One primary tenant per lease.
        builder.HasIndex(t => t.LeaseId, "IX_LeaseTenants_LeaseId_Primary")
            .IsUnique()
            .HasFilter("\"IsPrimary\" = true");

        builder.HasOne(t => t.Lease)
            .WithMany(l => l.Tenants)
            .HasForeignKey(t => t.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExtensionRequestConfiguration : IEntityTypeConfiguration<ExtensionRequest>
{
    public void Configure(EntityTypeBuilder<ExtensionRequest> builder)
    {
        builder.Property(r => r.TenantNote).HasMaxLength(1024);
        builder.Property(r => r.OwnerNote).HasMaxLength(1024);

        builder.HasIndex(r => new { r.LeaseId, r.Status });

        builder.HasOne(r => r.Lease)
            .WithMany()
            .HasForeignKey(r => r.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExtensionRequests_Extends",
            "\"RequestedEndDate\" > \"CurrentEndDate\""));
    }
}
