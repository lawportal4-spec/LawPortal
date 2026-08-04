using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Wallet.Queries;

public record GetWalletQuery : IRequest<WalletDto>;

public class GetWalletHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetWalletQuery, WalletDto>
{
    public async Task<WalletDto> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        return new WalletDto(wallet?.Balance ?? 0m, wallet?.CurrencyCode ?? "SAR");
    }
}

public record GetWalletTransactionsQuery : IRequest<IReadOnlyList<WalletTransactionDto>>;

public class GetWalletTransactionsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetWalletTransactionsQuery, IReadOnlyList<WalletTransactionDto>>
{
    public async Task<IReadOnlyList<WalletTransactionDto>> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet is null) return [];

        return await db.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(50)
            .Select(t => new WalletTransactionDto(t.Id, t.Type.ToString(), t.Amount, t.Description, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
