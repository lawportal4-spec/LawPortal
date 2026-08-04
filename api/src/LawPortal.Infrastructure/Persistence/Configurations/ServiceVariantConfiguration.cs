using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class ServiceVariantConfiguration : IEntityTypeConfiguration<ServiceVariant>
{
    public void Configure(EntityTypeBuilder<ServiceVariant> builder)
    {
        builder.ToTable("service_variants");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(v => v.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(v => v.BasePrice).HasColumnType("decimal(10,2)");
        builder.Property(v => v.ExtraUnitPrice).HasColumnType("decimal(10,2)");
        builder.Property(v => v.QuantityLabelAr).HasMaxLength(150);
        builder.Property(v => v.QuantityLabelEn).HasMaxLength(150);
        builder.HasOne(v => v.Service).WithMany(s => s.Variants).HasForeignKey(v => v.ServiceId);
    }
}
