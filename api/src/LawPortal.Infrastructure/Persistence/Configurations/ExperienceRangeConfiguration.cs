using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class ExperienceRangeConfiguration : IEntityTypeConfiguration<ExperienceRange>
{
    public void Configure(EntityTypeBuilder<ExperienceRange> builder)
    {
        builder.ToTable("experience_ranges");
        builder.HasKey(e => e.Id);
        builder.Ignore(e => e.Display);
    }
}
