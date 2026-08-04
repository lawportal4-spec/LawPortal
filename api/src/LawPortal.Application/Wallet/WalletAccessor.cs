using LawPortal.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using WalletEntity = LawPortal.Domain.Wallet.Wallet;

namespace LawPortal.Application.Wallet;

public static class WalletAccessor
{
    public static async Task<WalletEntity> GetOrCreateAsync(ILawPortalDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet is not null) return wallet;

        wallet = new WalletEntity { Id = Guid.NewGuid(), UserId = userId, Balance = 0m };
        db.Wallets.Add(wallet);
        return wallet;
    }
}
