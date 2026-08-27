using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;
using NetTopologySuite.Geometries;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class BoardingHouseConfiguration : IEntityTypeConfiguration<BoardingHouse>
{
    /// <summary>
    /// Longitude first. Verified against PostGIS 3.5.2: ST_MakePoint accepts the
    /// decimal(9,6) columns directly, no cast needed (docs/verification/erd-check.sql).
    /// </summary>
    private const string LocationSql =
        "ST_SetSRID(ST_MakePoint(\"Longitude\", \"Latitude\"), 4326)::geography";

    public void Configure(EntityTypeBuilder<BoardingHouse> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.AddressLine).HasMaxLength(256).IsRequired();
        builder.Property(b => b.Ward).HasMaxLength(100).IsRequired();
        builder.Property(b => b.District).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Province).HasMaxLength(100).IsRequired();
        builder.Property(b => b.RejectionReason).HasMaxLength(512);

        builder.Property(b => b.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(b => b.Longitude).HasColumnType("decimal(9,6)");
        builder.Property(b => b.Rating).HasColumnType("decimal(2,1)");

        // Shadow property: PostgreSQL owns this column and rejects any write to it, so it
        // is not exposed on the entity at all. Queries use EF.Property or raw SQL.
        builder.Property<Point>("Location")
            .HasColumnType("geography(Point,4326)")
            .HasComputedColumnSql(LocationSql, stored: true);

        builder.HasIndex("Location").HasMethod("gist");
        builder.HasIndex(b => new { b.ListingStatus, b.IsDeleted });
        builder.HasIndex(b => new { b.Province, b.District });
        builder.HasIndex(b => b.OwnerUserId);

        builder.HasOne(b => b.OwnerUser)
            .WithMany()
            .HasForeignKey(b => b.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_BoardingHouses_Latitude_Range",
                "\"Latitude\" BETWEEN -90 AND 90");
            t.HasCheckConstraint(
                "CK_BoardingHouses_Longitude_Range",
                "\"Longitude\" BETWEEN -180 AND 180");
        });
    }
}

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.Property(f => f.Name).HasMaxLength(100).IsRequired();
        builder.Property(f => f.CodeName).HasMaxLength(64).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(512);
        builder.Property(f => f.IconKey).HasMaxLength(64);

        builder.HasIndex(f => f.Name).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(f => f.CodeName).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.Property(t => t.TypeName).HasMaxLength(128).IsRequired();
        builder.Property(t => t.RoomSizeM2).HasColumnType("decimal(6,2)");
        builder.Property(t => t.Description).HasMaxLength(1024);

        builder.HasIndex(t => t.BoardingHouseId);

        builder.HasOne(t => t.BoardingHouse)
            .WithMany(b => b.RoomTypes)
            .HasForeignKey(t => t.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Facilities)
            .WithMany(f => f.RoomTypes)
            .UsingEntity("RoomTypeFacilities");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_RoomTypes_Price_NonNegative", "\"Price\" >= 0");
            t.HasCheckConstraint("CK_RoomTypes_MaxOccupants_Positive", "\"MaxOccupants\" >= 1");
        });
    }
}

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.Property(r => r.RoomNumber).HasMaxLength(32).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(1024);
        builder.Property(r => r.CurrentElectricityReading).HasColumnType("decimal(12,2)");
        builder.Property(r => r.CurrentWaterReading).HasColumnType("decimal(12,2)");

        // Verified: soft-deleting a room frees its number for reuse.
        builder.HasIndex(r => new { r.BoardingHouseId, r.RoomNumber })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(r => new { r.BoardingHouseId, r.Status });

        builder.HasOne(r => r.BoardingHouse)
            .WithMany(b => b.Rooms)
            .HasForeignKey(r => r.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RoomType)
            .WithMany(t => t.Rooms)
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
