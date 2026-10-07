using LawPortal.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Number).HasMaxLength(30).IsRequired();
        builder.HasIndex(p => p.Number).IsUnique();

        builder.Property(p => p.GrossAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.DiscountAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.Total).HasColumnType("decimal(10,2)");
        builder.Property(p => p.VatAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.CommissionAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.NetToLawyerAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.CurrencyCode).HasMaxLength(3);
        builder.Property(p => p.MethodDescription).HasMaxLength(50);
        builder.Property(p => p.GatewayProvider).HasMaxLength(30);
        builder.Property(p => p.GatewayPaymentId).HasMaxLength(100);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.OwnsOne(p => p.Transaction, t =>
        {
            t.Property(x => x.TransactionId).HasColumnName("GwTransactionId").HasMaxLength(100);
            t.Property(x => x.SourceType).HasColumnName("GwSourceType").HasMaxLength(30);
            t.Property(x => x.CardBrand).HasColumnName("GwCardBrand").HasMaxLength(30);
            t.Property(x => x.CardMasked).HasColumnName("GwCardMasked").HasMaxLength(30);
            t.Property(x => x.ReferenceNumber).HasColumnName("GwReferenceNumber").HasMaxLength(50);
            t.Property(x => x.AuthorizationCode).HasColumnName("GwAuthorizationCode").HasMaxLength(30);
            t.Property(x => x.ResponseCode).HasColumnName("GwResponseCode").HasMaxLength(20);
            t.Property(x => x.Message).HasColumnName("GwMessage").HasMaxLength(200);
            t.Property(x => x.Fee).HasColumnName("GwFee").HasColumnType("decimal(10,2)");
            t.Property(x => x.FetchedAtUtc).HasColumnName("GwFetchedAtUtc");
        });

        builder.HasOne(p => p.ServiceRequest).WithMany().HasForeignKey(p => p.ServiceRequestId);
        builder.HasOne(p => p.Client).WithMany().HasForeignKey(p => p.ClientId);
        builder.HasOne(p => p.LawyerProfile).WithMany().HasForeignKey(p => p.LawyerProfileId);
        builder.HasMany(p => p.Refunds).WithOne(r => r.Payment!).HasForeignKey(r => r.PaymentId);
        builder.HasOne(p => p.Payout).WithOne(o => o.Payment!).HasForeignKey<Payout>(o => o.PaymentId);

        builder.HasIndex(p => p.GatewayPaymentId);
        builder.HasIndex(p => new { p.ClientId, p.Status });
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasColumnType("decimal(10,2)");
        builder.Property(r => r.Details).HasMaxLength(500);
        builder.Property(r => r.GatewayRefundId).HasMaxLength(100);
    }
}

public class LawyerDebtEntryConfiguration : IEntityTypeConfiguration<LawyerDebtEntry>
{
    public void Configure(EntityTypeBuilder<LawyerDebtEntry> builder)
    {
        builder.ToTable("lawyer_debt_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasColumnType("decimal(10,2)");
        builder.Property(e => e.Reference).HasMaxLength(100);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.HasIndex(e => e.LawyerProfileId);
    }
}

public class RefundPolicySettingConfiguration : IEntityTypeConfiguration<RefundPolicySetting>
{
    public void Configure(EntityTypeBuilder<RefundPolicySetting> builder)
    {
        builder.ToTable("refund_policy_settings");
        builder.Property(s => s.Id).ValueGeneratedNever();
    }
}

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("payouts");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Amount).HasColumnType("decimal(10,2)");
        builder.Property(o => o.DebtOffset).HasColumnType("decimal(10,2)");
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.HasOne(o => o.LawyerProfile).WithMany().HasForeignKey(o => o.LawyerProfileId);
    }
}
