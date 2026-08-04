using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Commands;

/// <summary>Awards the request to one lawyer — sets the price (from the offer's latest
/// revision), rejects every other still-pending offer so no other lawyer can counter or be
/// accepted afterward, and moves the request to <see cref="RequestStatus.Awarded"/>. Checkout
/// (<c>InitiateCheckoutCommand</c>) is a separate, later client action, same as every other
/// request shape.</summary>
public record AcceptOfferCommand(Guid OfferId) : IRequest<Unit>;

public class AcceptOfferHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<AcceptOfferCommand, Unit>
{
    public async Task<Unit> Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var offer = await db.Offers
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken)
            ?? throw new KeyNotFoundException("Offer not found.");

        var biddingRequest = await db.BiddingRequests
            .FirstOrDefaultAsync(b => b.Id == offer.ServiceRequestId && b.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Offer not found.");

        if (offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException($"This offer is {offer.Status} and can no longer be accepted.");

        if (offer.ExpiresAtUtc < DateTime.UtcNow)
            throw new InvalidOperationException("This offer has expired.");

        var latestAmount = await db.OfferRevisions
            .Where(r => r.OfferId == offer.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => r.Amount)
            .FirstAsync(cancellationToken);

        offer.Status = OfferStatus.Accepted;

        var otherPendingOffers = await db.Offers
            .Where(o => o.ServiceRequestId == biddingRequest.Id && o.Id != offer.Id && o.Status == OfferStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var other in otherPendingOffers) other.Status = OfferStatus.Rejected;

        biddingRequest.AwardedLawyerProfileId = offer.LawyerProfileId;
        biddingRequest.AwardedOfferId = offer.Id;
        biddingRequest.Subtotal = latestAmount;

        db.RequestStatusHistories.Add(biddingRequest.TransitionTo(RequestStatus.Awarded, "ClientAcceptedOffer"));

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
