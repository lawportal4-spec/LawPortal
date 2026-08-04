using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.NameAr).HasMaxLength(120).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(120).IsRequired();
        builder.HasIndex(c => c.RegionId);
    }
}
