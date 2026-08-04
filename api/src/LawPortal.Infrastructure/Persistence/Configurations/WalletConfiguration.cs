using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletEntity = LawPortal.Domain.Wallet.Wallet;
using WalletTransactionEntity = LawPortal.Domain.Wallet.WalletTransaction;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<WalletEntity>
{
    public void Configure(EntityTypeBuilder<WalletEntity> builder)
    {
        builder.ToTable("wallets");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Balance).HasColumnType("decimal(10,2)");
        builder.Property(w => w.CurrencyCode).HasMaxLength(3);
        builder.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId);
        builder.HasIndex(w => w.UserId).IsUnique();
        builder.HasMany(w => w.Transactions).WithOne(t => t.Wallet!).HasForeignKey(t => t.WalletId);
    }
}

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransactionEntity>
{
    public void Configure(EntityTypeBuilder<WalletTransactionEntity> builder)
    {
        builder.ToTable("wallet_transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasColumnType("decimal(10,2)");
        builder.Property(t => t.Description).HasMaxLength(500);
    }
}
