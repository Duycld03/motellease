using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class PaymentBillConfiguration : IEntityTypeConfiguration<PaymentBill>
{
    public void Configure(EntityTypeBuilder<PaymentBill> builder)
    {
        foreach (var reading in new[]
                 {
                     nameof(PaymentBill.ElectricityOld), nameof(PaymentBill.ElectricityNew),
                     nameof(PaymentBill.ElectricityQty), nameof(PaymentBill.WaterOld),
                     nameof(PaymentBill.WaterNew), nameof(PaymentBill.WaterQty)
                 })
        {
            builder.Property(reading).HasColumnType("decimal(12,2)");
        }

        // Invariant §9.4: one bill per room per month.
        builder.HasIndex(b => new { b.RoomId, b.Month, b.Year }).IsUnique();
        builder.HasIndex(b => new { b.Status, b.DueDate });
        builder.HasIndex(b => new { b.LeaseId, b.Year, b.Month });

        builder.HasOne(b => b.Lease)
            .WithMany(l => l.Bills)
            .HasForeignKey(b => b.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Room)
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PaymentBills_Month_Range", "\"Month\" BETWEEN 1 AND 12");
            // Invariant §9.9: a meter cannot run backwards.
            t.HasCheckConstraint(
                "CK_PaymentBills_ElectricityReadings",
                "\"ElectricityNew\" >= \"ElectricityOld\"");
            t.HasCheckConstraint(
                "CK_PaymentBills_WaterReadings",
                "\"WaterNew\" >= \"WaterOld\"");
            // Invariant §9.5: the total is the sum of its parts, enforced by the database
            // so a buggy handler cannot issue an inconsistent bill.
            t.HasCheckConstraint(
                "CK_PaymentBills_TotalAmount_Sum",
                "\"TotalAmount\" = \"RentAmount\" + \"ElectricityAmount\" + \"WaterAmount\" + \"AdditionalFeeTotal\"");
        });
    }
}

public class RoomAdditionalFeeConfiguration : IEntityTypeConfiguration<RoomAdditionalFee>
{
    public void Configure(EntityTypeBuilder<RoomAdditionalFee> builder)
    {
        builder.Property(f => f.FeeName).HasMaxLength(128).IsRequired();

        builder.HasIndex(f => new { f.RoomId, f.Year, f.Month });
        builder.HasIndex(f => f.PaymentBillId);

        builder.HasOne(f => f.Room)
            .WithMany()
            .HasForeignKey(f => f.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.PaymentBill)
            .WithMany(b => b.AdditionalFees)
            .HasForeignKey(f => f.PaymentBillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_RoomAdditionalFees_Month_Range", "\"Month\" BETWEEN 1 AND 12");
            t.HasCheckConstraint("CK_RoomAdditionalFees_Amount_NonNegative", "\"FeeAmount\" >= 0");
        });
    }
}

public class BoardingHouseExpenseConfiguration : IEntityTypeConfiguration<BoardingHouseExpense>
{
    public void Configure(EntityTypeBuilder<BoardingHouseExpense> builder)
    {
        foreach (var reading in new[]
                 {
                     nameof(BoardingHouseExpense.ElectricityOld),
                     nameof(BoardingHouseExpense.ElectricityNew),
                     nameof(BoardingHouseExpense.ElectricityQty),
                     nameof(BoardingHouseExpense.WaterOld),
                     nameof(BoardingHouseExpense.WaterNew),
                     nameof(BoardingHouseExpense.WaterQty)
                 })
        {
            builder.Property(reading).HasColumnType("decimal(12,2)");
        }

        builder.Property(e => e.OtherExpenses).HasColumnType("jsonb");

        builder.HasIndex(e => new { e.BoardingHouseId, e.Month, e.Year }).IsUnique();

        builder.HasOne(e => e.BoardingHouse)
            .WithMany()
            .HasForeignKey(e => e.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_BoardingHouseExpenses_Month_Range",
                "\"Month\" BETWEEN 1 AND 12");
            t.HasCheckConstraint(
                "CK_BoardingHouseExpenses_TotalExpense_Sum",
                "\"TotalExpense\" = \"ElectricityAmount\" + \"WaterAmount\" + \"OtherExpensesTotal\"");
        });
    }
}
