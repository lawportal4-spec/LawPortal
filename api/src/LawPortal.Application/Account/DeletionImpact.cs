using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Account;

/// <summary>What the person loses or still owes if they delete their account — shown before they confirm.
/// Deleting is never blocked by it.</summary>
/// <param name="HeldPayoutsTotal">Lawyer: shares not paid to them yet, which stop on deletion.</param>
/// <param name="DebtBalance">Lawyer: what they owe the platform, which is still collected after deletion.</param>
/// <param name="WalletBalance">Client: wallet balance that is lost.</param>
/// <param name="OpenRequests">Requests still in progress.</param>
public record DeletionImpactDto(string UserType, decimal HeldPayoutsTotal, int HeldPayoutsCount, decimal DebtBalance, decimal WalletBalance, int OpenRequests);

public record GetDeletionImpactQuery : IRequest<DeletionImpactDto>;

public class GetDeletionImpactHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetDeletionImpactQuery, DeletionImpactDto>
{

    public async Task<DeletionImpactDto> Handle(GetDeletionImpactQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var user = await db.Users.Include(u => u.LawyerProfile).Include(u => u.ClientProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken) ?? throw new UnauthorizedAccessException();

        if (user.LawyerProfile is { } lawyer)
        {
            var held = await db.Payouts.Where(o => o.LawyerProfileId == lawyer.Id && o.Status == PayoutStatus.Held)
                .Select(o => o.Amount).ToListAsync(cancellationToken);
            // Each held payout is a paid request the lawyer hasn't completed yet.
            return new DeletionImpactDto(user.UserType.ToString(), held.Sum(), held.Count,
                await LawyerDebts.BalanceAsync(db, lawyer.Id, cancellationToken), 0, held.Count);
        }

        var wallet = await db.Wallets.Where(w => w.UserId == userId).Select(w => (decimal?)w.Balance).FirstOrDefaultAsync(cancellationToken) ?? 0;
        var clientOpen = user.ClientProfile is { } client
            ? await db.ServiceRequests.CountAsync(r => r.ClientId == client.Id && r.Status != RequestStatus.Completed && r.Status != RequestStatus.Cancelled && r.Status != RequestStatus.Refunded && r.Status != RequestStatus.Draft, cancellationToken)
            : 0;
        return new DeletionImpactDto(user.UserType.ToString(), 0, 0, 0, wallet, clientOpen);
    }
}
