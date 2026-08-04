using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("regions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.NameAr).HasMaxLength(120).IsRequired();
        builder.Property(r => r.NameEn).HasMaxLength(120).IsRequired();
        builder.HasMany(r => r.Cities).WithOne(c => c.Region).HasForeignKey(c => c.RegionId);
    }
}
