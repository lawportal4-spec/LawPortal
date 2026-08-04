using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Code).HasMaxLength(8).IsRequired();
        builder.HasIndex(l => l.Code).IsUnique();
        builder.Property(l => l.NameAr).HasMaxLength(60).IsRequired();
        builder.Property(l => l.NameEn).HasMaxLength(60).IsRequired();
    }
}
