using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.TitleKey).HasMaxLength(128).IsRequired();
        builder.Property(n => n.BodyKey).HasMaxLength(128).IsRequired();
        builder.Property(n => n.PayloadJson).HasColumnType("jsonb");
        builder.Property(n => n.LinkUrl).HasMaxLength(512);

        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .IsDescending(false, false, true);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(l => l.Action).HasMaxLength(128).IsRequired();
        builder.Property(l => l.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(l => l.BeforeJson).HasColumnType("jsonb");
        builder.Property(l => l.AfterJson).HasColumnType("jsonb");
        builder.Property(l => l.IpAddress).HasMaxLength(64);

        builder.HasIndex(l => new { l.EntityType, l.EntityId, l.CreatedAt })
            .IsDescending(false, false, true);
        builder.HasIndex(l => new { l.ActorUserId, l.CreatedAt })
            .IsDescending(false, true);

        // No FK to Users: the log must survive the deletion of the actor's account.
    }
}
