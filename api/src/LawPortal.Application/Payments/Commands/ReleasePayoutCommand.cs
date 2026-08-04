using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments.Commands;

/// <summary>Admin-triggered stopgap — the real trigger is a lawyer marking work complete, which
/// needs the lawyer dashboard (P6) to exist first. This lets the escrow-hold-then-release ledger
/// path be built and verified now rather than left theoretical.</summary>
public record ReleasePayoutCommand(Guid PayoutId) : IRequest<Unit>;

public class ReleasePayoutHandler(ILawPortalDbContext db) : IRequestHandler<ReleasePayoutCommand, Unit>
{
    public async Task<Unit> Handle(ReleasePayoutCommand request, CancellationToken cancellationToken)
    {
        var payout = await db.Payouts.FirstOrDefaultAsync(o => o.Id == request.PayoutId, cancellationToken)
            ?? throw new KeyNotFoundException("Payout not found.");

        if (payout.Status != PayoutStatus.Held)
            throw new InvalidOperationException("This payout has already been released.");

        payout.Status = PayoutStatus.Released;
        payout.ReleasedAtUtc = DateTime.UtcNow;

        db.LedgerEntries.AddRange(LedgerPostingService.PostPayout(payout));

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
