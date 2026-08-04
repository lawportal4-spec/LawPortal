using LawPortal.Domain.Identity;
using LawPortal.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class LawyerSpecialtyConfiguration : IEntityTypeConfiguration<LawyerSpecialty>
{
    public void Configure(EntityTypeBuilder<LawyerSpecialty> builder)
    {
        builder.ToTable("lawyer_specialties");
        builder.HasKey(ls => new { ls.LawyerProfileId, ls.SpecialtyId });
        builder.HasOne(ls => ls.LawyerProfile).WithMany(l => l.LawyerSpecialties).HasForeignKey(ls => ls.LawyerProfileId);
        builder.HasOne(ls => ls.Specialty).WithMany().HasForeignKey(ls => ls.SpecialtyId);
        builder.HasIndex(ls => ls.SpecialtyId);
    }
}

public class LawyerLanguageConfiguration : IEntityTypeConfiguration<LawyerLanguage>
{
    public void Configure(EntityTypeBuilder<LawyerLanguage> builder)
    {
        builder.ToTable("lawyer_languages");
        builder.HasKey(ll => new { ll.LawyerProfileId, ll.LanguageId });
        builder.HasOne(ll => ll.LawyerProfile).WithMany(l => l.LawyerLanguages).HasForeignKey(ll => ll.LawyerProfileId);
        builder.HasOne(ll => ll.Language).WithMany().HasForeignKey(ll => ll.LanguageId);
    }
}

public class LawyerQualificationConfiguration : IEntityTypeConfiguration<LawyerQualification>
{
    public void Configure(EntityTypeBuilder<LawyerQualification> builder)
    {
        builder.ToTable("lawyer_qualifications");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.TitleAr).HasMaxLength(300).IsRequired();
        builder.Property(q => q.TitleEn).HasMaxLength(300).IsRequired();
        builder.Property(q => q.Institution).HasMaxLength(300);
        builder.HasOne(q => q.LawyerProfile).WithMany(l => l.Qualifications).HasForeignKey(q => q.LawyerProfileId);
    }
}

public class LawyerPricingConfiguration : IEntityTypeConfiguration<LawyerPricing>
{
    public void Configure(EntityTypeBuilder<LawyerPricing> builder)
    {
        builder.ToTable("lawyer_pricing");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.WrittenPrice).HasColumnType("decimal(10,2)");
        builder.Property(p => p.Price15).HasColumnType("decimal(10,2)");
        builder.Property(p => p.Price30).HasColumnType("decimal(10,2)");
        builder.Property(p => p.Price45).HasColumnType("decimal(10,2)");
        builder.HasOne(p => p.LawyerProfile).WithOne(l => l.Pricing!).HasForeignKey<LawyerPricing>(p => p.LawyerProfileId);
        builder.HasIndex(p => p.LawyerProfileId).IsUnique();
    }
}
