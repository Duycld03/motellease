using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(a => a.Note).HasMaxLength(1024);
        builder.Property(a => a.ReasonForCancel).HasMaxLength(512);

        builder.HasIndex(a => new { a.RoomId, a.AppointmentDate });
        builder.HasIndex(a => new { a.UserId, a.Status });

        builder.HasOne(a => a.Room)
            .WithMany()
            .HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        builder.Property(d => d.ReasonForCancel).HasMaxLength(512);

        builder.HasIndex(d => new { d.RoomId, d.Status });
        builder.HasIndex(d => new { d.UserId, d.Status });

        // Supports "is this room already held?" without scanning the room's history.
        builder.HasIndex(d => d.RoomId, "IX_Deposits_RoomId_Holding")
            .HasFilter("\"Status\" IN ('Accepted', 'Paid')");

        builder.HasOne(d => d.Room)
            .WithMany()
            .HasForeignKey(d => d.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Deposits_Amount_Positive", "\"Amount\" > 0");
            t.HasCheckConstraint(
                "CK_Deposits_RequestedTermMonths_Positive",
                "\"RequestedTermMonths\" >= 1");
        });
    }
}
