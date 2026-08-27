using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Username).HasMaxLength(64).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(128).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.SocialId).HasMaxLength(128);
        builder.Property(u => u.AvatarUrl).HasMaxLength(512);
        builder.Property(u => u.AvatarPublicId).HasMaxLength(256);
        builder.Property(u => u.PreferredLanguage).HasMaxLength(2).IsRequired();
        builder.Property(u => u.LockedReason).HasMaxLength(512);

        // Partial: a soft-deleted account must not reserve its email or username forever.
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(u => u.Username).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(u => u.SocialId)
            .IsUnique()
            .HasFilter("\"SocialId\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(u => new { u.Role, u.IsDeleted });

        builder.HasOne(u => u.OwnerProfile)
            .WithOne(p => p.User)
            .HasForeignKey<OwnerProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.StaffProfile)
            .WithOne(p => p.User)
            .HasForeignKey<StaffProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OwnerProfileConfiguration : IEntityTypeConfiguration<OwnerProfile>
{
    public void Configure(EntityTypeBuilder<OwnerProfile> builder)
    {
        builder.Property(p => p.BusinessName).HasMaxLength(200);
        builder.Property(p => p.BankName).HasMaxLength(100);
        builder.Property(p => p.BankAccountNumber).HasMaxLength(32);
        builder.Property(p => p.BankAccountHolder).HasMaxLength(128);

        builder.HasIndex(p => p.UserId).IsUnique();

        // A negative balance would mean money was paid out that never came in.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_OwnerProfiles_AvailableBalance_NonNegative",
            "\"AvailableBalance\" >= 0"));
    }
}

public class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> builder)
    {
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasIndex(p => p.CreatedByUserId);
    }
}
