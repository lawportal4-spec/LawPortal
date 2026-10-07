using LawPortal.Domain.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class CommissionPolicyConfiguration : IEntityTypeConfiguration<CommissionPolicy>
{
    public void Configure(EntityTypeBuilder<CommissionPolicy> builder)
    {
        builder.ToTable("commission_policies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.ServiceCategorySlug).HasMaxLength(60);
        builder.Property(p => p.Percentage).HasColumnType("decimal(5,2)");
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Number).HasMaxLength(30).IsRequired();
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.PaymentId).IsUnique();

        builder.Property(i => i.SubtotalExVat).HasColumnType("decimal(10,2)");
        builder.Property(i => i.DiscountAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.VatAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(10,2)");
        builder.Property(i => i.SellerNameAr).HasMaxLength(200);
        builder.Property(i => i.SellerNameEn).HasMaxLength(200);
        builder.Property(i => i.SellerVatNumber).HasMaxLength(30);
        builder.Property(i => i.QrPayloadBase64).HasColumnType("text");
    }
}

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.ToTable("discount_codes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();
        builder.Property(d => d.DescriptionAr).HasMaxLength(300);
        builder.Property(d => d.DescriptionEn).HasMaxLength(300);
        builder.Property(d => d.Value).HasColumnType("decimal(10,2)");
        builder.Property(d => d.MaxDiscountAmount).HasColumnType("decimal(10,2)");
        builder.Property(d => d.MinAmount).HasColumnType("decimal(10,2)");
        builder.HasMany(d => d.Redemptions).WithOne(r => r.DiscountCode).HasForeignKey(r => r.DiscountCodeId);
    }
}

public class DiscountRedemptionConfiguration : IEntityTypeConfiguration<DiscountRedemption>
{
    public void Configure(EntityTypeBuilder<DiscountRedemption> builder)
    {
        builder.ToTable("discount_redemptions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasColumnType("decimal(10,2)");
        builder.HasIndex(r => new { r.DiscountCodeId, r.Status });
        builder.HasIndex(r => new { r.DiscountCodeId, r.UserId });
        builder.HasIndex(r => r.ReferenceId);
    }
}

public class RegistrationFeeSettingConfiguration : IEntityTypeConfiguration<RegistrationFeeSetting>
{
    public void Configure(EntityTypeBuilder<RegistrationFeeSetting> builder)
    {
        builder.ToTable("registration_fee_settings");
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Amount).HasColumnType("decimal(10,2)");
    }
}

public class LawyerRegistrationFeeInvoiceConfiguration : IEntityTypeConfiguration<LawyerRegistrationFeeInvoice>
{
    public void Configure(EntityTypeBuilder<LawyerRegistrationFeeInvoice> builder)
    {
        builder.ToTable("registration_fee_invoices");
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.GatewayPaymentId);
        builder.HasIndex(i => i.LawyerProfileId);
        builder.Property(i => i.Number).HasMaxLength(30);
        builder.Property(i => i.BaseAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.DiscountAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.SubtotalExVat).HasColumnType("decimal(10,2)");
        builder.Property(i => i.VatAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(10,2)");
        builder.Property(i => i.GatewayProvider).HasMaxLength(30);
        builder.Property(i => i.GatewayPaymentId).HasMaxLength(100);
        builder.Property(i => i.FailureReason).HasMaxLength(300);
    }
}

