using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.NameAr).HasMaxLength(120).IsRequired();
        builder.Property(s => s.NameEn).HasMaxLength(120).IsRequired();
        builder.Property(s => s.Slug).HasMaxLength(120).IsRequired();
        builder.HasIndex(s => s.Slug).IsUnique();
        builder.HasMany(s => s.SubSpecialties).WithOne(sub => sub.Specialty).HasForeignKey(sub => sub.SpecialtyId);
    }
}

public class SubSpecialtyConfiguration : IEntityTypeConfiguration<SubSpecialty>
{
    public void Configure(EntityTypeBuilder<SubSpecialty> builder)
    {
        builder.ToTable("sub_specialties");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.NameAr).HasMaxLength(120).IsRequired();
        builder.Property(s => s.NameEn).HasMaxLength(120).IsRequired();
        builder.HasIndex(s => s.SpecialtyId);
    }
}
