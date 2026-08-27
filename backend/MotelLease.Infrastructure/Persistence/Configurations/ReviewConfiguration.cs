using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(r => r.Content).HasMaxLength(2048).IsRequired();

        builder.HasIndex(r => new { r.BoardingHouseId, r.IsDeleted });
        builder.HasIndex(r => r.ParentReviewId);

        // Invariant §9.10: one review per lease. Replies are excluded from the constraint.
        builder.HasIndex(r => new { r.UserId, r.LeaseId })
            .IsUnique()
            .HasFilter("\"ParentReviewId\" IS NULL AND \"IsDeleted\" = false");

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.BoardingHouse)
            .WithMany()
            .HasForeignKey(r => r.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Lease)
            .WithMany()
            .HasForeignKey(r => r.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ParentReview)
            .WithMany(r => r.Replies)
            .HasForeignKey(r => r.ParentReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        // A reply carries no score; a review always does.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Reviews_Rating",
            "(\"ParentReviewId\" IS NULL AND \"Rating\" BETWEEN 1 AND 5) " +
            "OR (\"ParentReviewId\" IS NOT NULL AND \"Rating\" IS NULL)"));
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.Property(r => r.Reason).HasMaxLength(256).IsRequired();
        builder.Property(r => r.Details).HasMaxLength(2048);
        builder.Property(r => r.Resolution).HasMaxLength(1024);

        builder.HasIndex(r => new { r.TargetType, r.TargetId });
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        builder.HasOne(r => r.ReporterUser)
            .WithMany()
            .HasForeignKey(r => r.ReporterUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
