using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.UserAgent).HasMaxLength(256);
        builder.Property(t => t.IpAddress).HasMaxLength(64);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.RevokedAt });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
{
    public void Configure(EntityTypeBuilder<StaffAssignment> builder)
    {
        // Partial: the same staff member may be re-assigned to a house they once left,
        // but only one assignment can be live at a time.
        builder.HasIndex(a => new { a.BoardingHouseId, a.StaffUserId })
            .IsUnique()
            .HasFilter("\"UnassignedAt\" IS NULL");
        builder.HasIndex(a => a.StaffUserId);

        builder.HasOne(a => a.BoardingHouse)
            .WithMany()
            .HasForeignKey(a => a.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.StaffUser)
            .WithMany()
            .HasForeignKey(a => a.StaffUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.Property(i => i.Url).HasMaxLength(512).IsRequired();
        builder.Property(i => i.PublicId).HasMaxLength(256).IsRequired();

        builder.HasIndex(i => new { i.OwnerType, i.OwnerId });

        // At most one cover image per owner. Named overload: an unnamed HasIndex on the same
        // columns would reconfigure the lookup index above instead of adding a second one.
        builder.HasIndex(i => new { i.OwnerType, i.OwnerId }, "IX_Images_Owner_Primary")
            .IsUnique()
            .HasFilter("\"IsPrimary\" = true");
    }
}

public class SavedListingConfiguration : IEntityTypeConfiguration<SavedListing>
{
    public void Configure(EntityTypeBuilder<SavedListing> builder)
    {
        builder.HasIndex(s => new { s.UserId, s.BoardingHouseId }).IsUnique();

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.BoardingHouse)
            .WithMany()
            .HasForeignKey(s => s.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
