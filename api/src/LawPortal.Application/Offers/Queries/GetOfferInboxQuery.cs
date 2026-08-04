using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Offers.Dtos;
using LawPortal.Application.Requests.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Queries;

/// <summary>The client's offer inbox for one bidding request — every lawyer's offer, its full
/// negotiation history, and the lawyer's public reputation stats, so the client can decide who
/// to accept without leaving the page.</summary>
public record GetOfferInboxQuery(Guid RequestId) : IRequest<IReadOnlyList<OfferSummaryDto>>;

public class GetOfferInboxHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetOfferInboxQuery, IReadOnlyList<OfferSummaryDto>>
{
    public async Task<IReadOnlyList<OfferSummaryDto>> Handle(GetOfferInboxQuery request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var owned = await db.BiddingRequests.AnyAsync(b => b.Id == request.RequestId && b.ClientId == clientId, cancellationToken);
        if (!owned) throw new KeyNotFoundException("Request not found.");

        var offers = await db.Offers
            .Where(o => o.ServiceRequestId == request.RequestId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<OfferSummaryDto>(offers.Count);
        foreach (var offer in offers)
        {
            var lawyer = await db.LawyerProfiles.FirstAsync(l => l.Id == offer.LawyerProfileId, cancellationToken);
            var revisions = await db.OfferRevisions
                .Where(r => r.OfferId == offer.Id)
                .OrderBy(r => r.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            result.Add(new OfferSummaryDto(
                offer.Id,
                lawyer.Id,
                lawyer.FullName,
                lawyer.AvgRating,
                lawyer.RatingCount,
                lawyer.CompletedRequestCount,
                offer.Status.ToString(),
                revisions[^1].Amount,
                offer.ExpiresAtUtc,
                revisions.Select(r => new OfferRevisionDto(r.Id, r.Amount, r.ProposedBy.ToString(), r.Message, r.CreatedAtUtc)).ToList()));
        }

        return result;
    }
}
