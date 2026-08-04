using LawPortal.Domain.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasColumnType("decimal(10,2)");
        builder.Property(e => e.CurrencyCode).HasMaxLength(3);
        builder.Property(e => e.ReferenceType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId });
        builder.HasIndex(e => e.Account);
    }
}
