using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.Property(t => t.ProviderOrderId).HasMaxLength(128).IsRequired();
        builder.Property(t => t.ProviderTxnId).HasMaxLength(128);
        builder.Property(t => t.RawCallbackPayload).HasColumnType("jsonb");

        builder.HasIndex(t => t.ProviderOrderId).IsUnique();

        // Invariant §9.7: a replayed IPN callback carries the same gateway id and is
        // rejected by the database even if the handler check is bypassed.
        builder.HasIndex(t => t.ProviderTxnId)
            .IsUnique()
            .HasFilter("\"ProviderTxnId\" IS NOT NULL");
        builder.HasIndex(t => new { t.Status, t.InitiatedAt });
        builder.HasIndex(t => t.DepositId);
        builder.HasIndex(t => t.PaymentBillId);
        builder.HasIndex(t => t.RefundRequestId);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Deposit)
            .WithMany()
            .HasForeignKey(t => t.DepositId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.PaymentBill)
            .WithMany()
            .HasForeignKey(t => t.PaymentBillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.RefundRequest)
            .WithMany()
            .HasForeignKey(t => t.RefundRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PaymentTransactions_Amount_Positive", "\"Amount\" > 0");
            // Exactly one target, so a transaction can never credit two things at once.
            t.HasCheckConstraint(
                "CK_PaymentTransactions_SingleTarget",
                "(CASE WHEN \"DepositId\" IS NULL THEN 0 ELSE 1 END + " +
                "CASE WHEN \"PaymentBillId\" IS NULL THEN 0 ELSE 1 END + " +
                "CASE WHEN \"RefundRequestId\" IS NULL THEN 0 ELSE 1 END) = 1");
        });
    }
}

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.Property(r => r.Reason).HasMaxLength(1024);
        builder.Property(r => r.RejectReason).HasMaxLength(512);

        builder.HasIndex(r => new { r.UserId, r.Status });
        builder.HasIndex(r => r.DepositId);
        builder.HasIndex(r => r.CreatedAt).IsDescending();

        builder.HasOne(r => r.Deposit)
            .WithMany()
            .HasForeignKey(r => r.DepositId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Lease)
            .WithMany()
            .HasForeignKey(r => r.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_RefundRequests_Amount_Positive",
            "\"Amount\" > 0"));
    }
}

public class WithdrawRequestConfiguration : IEntityTypeConfiguration<WithdrawRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawRequest> builder)
    {
        builder.Property(r => r.BankName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.BankAccountNumber).HasMaxLength(32).IsRequired();
        builder.Property(r => r.BankAccountHolder).HasMaxLength(128).IsRequired();
        builder.Property(r => r.RejectReason).HasMaxLength(512);

        builder.HasIndex(r => new { r.OwnerUserId, r.Status });
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        builder.HasOne(r => r.OwnerUser)
            .WithMany()
            .HasForeignKey(r => r.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WithdrawRequests_Amount_Positive",
            "\"Amount\" > 0"));
    }
}
