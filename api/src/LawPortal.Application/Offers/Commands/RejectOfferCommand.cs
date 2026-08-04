using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Commands;

/// <summary>The client declining one lawyer's offer without awarding anyone — the offer can no
/// longer be countered or accepted afterward, but the request stays Submitted and open to the
/// remaining offers.</summary>
public record RejectOfferCommand(Guid OfferId) : IRequest<Unit>;

public class RejectOfferHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<RejectOfferCommand, Unit>
{
    public async Task<Unit> Handle(RejectOfferCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var offer = await db.Offers
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken)
            ?? throw new KeyNotFoundException("Offer not found.");

        var owned = await db.BiddingRequests.AnyAsync(b => b.Id == offer.ServiceRequestId && b.ClientId == clientId, cancellationToken);
        if (!owned) throw new KeyNotFoundException("Offer not found.");

        if (offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException($"This offer is already {offer.Status}.");

        offer.Status = OfferStatus.Rejected;
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
