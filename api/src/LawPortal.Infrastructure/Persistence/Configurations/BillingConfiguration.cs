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
        builder.Property(i => i.VatAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(10,2)");
        builder.Property(i => i.SellerNameAr).HasMaxLength(200);
        builder.Property(i => i.SellerNameEn).HasMaxLength(200);
        builder.Property(i => i.SellerVatNumber).HasMaxLength(30);
        builder.Property(i => i.QrPayloadBase64).HasColumnType("text");
    }
}
