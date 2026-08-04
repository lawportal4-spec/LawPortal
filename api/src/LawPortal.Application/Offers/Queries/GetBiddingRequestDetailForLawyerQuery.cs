using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Offers.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Queries;

/// <summary>The lawyer's read of one bidding request they're invited to. Title and description
/// are visible to every invited lawyer (bidding lawyers price from the description — the FAQ's
/// own implication); attachments unlock only once this lawyer's offer has actually been
/// accepted, per the plan's resolution of the "970 strangers can't see privileged documents"
/// contradiction.</summary>
public record GetBiddingRequestDetailForLawyerQuery(Guid RequestId) : IRequest<BiddingRequestDetailForLawyerDto>;

public class GetBiddingRequestDetailForLawyerHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetBiddingRequestDetailForLawyerQuery, BiddingRequestDetailForLawyerDto>
{
    public async Task<BiddingRequestDetailForLawyerDto> Handle(GetBiddingRequestDetailForLawyerQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var isInvited = await db.RequestInvitations
            .AnyAsync(i => i.ServiceRequestId == request.RequestId && i.LawyerProfileId == lawyerProfileId, cancellationToken);
        if (!isInvited) throw new KeyNotFoundException("Request not found.");

        var bidding = await db.BiddingRequests
            .Include(b => b.Attachments)
            .FirstOrDefaultAsync(b => b.Id == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        var service = await db.ServiceCatalogItems.FirstAsync(s => s.Id == bidding.ServiceId, cancellationToken);
        var specialty = bidding.SpecialtyId is { } specialtyId
            ? await db.Specialties.FirstOrDefaultAsync(s => s.Id == specialtyId, cancellationToken)
            : null;

        var myOffer = await db.Offers
            .Where(o => o.ServiceRequestId == bidding.Id && o.LawyerProfileId == lawyerProfileId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        decimal? myLatestAmount = myOffer is null ? null : await db.OfferRevisions
            .Where(r => r.OfferId == myOffer.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => (decimal?)r.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        var attachmentsUnlocked = bidding.AwardedLawyerProfileId == lawyerProfileId;

        return new BiddingRequestDetailForLawyerDto(
            bidding.Id, bidding.Number, service.NameAr, service.NameEn,
            specialty?.NameAr, specialty?.NameEn, bidding.Title, bidding.Description, bidding.Status.ToString(),
            attachmentsUnlocked,
            attachmentsUnlocked ? bidding.Attachments.Select(a => a.FileName).ToList() : [],
            myOffer?.Status.ToString(), myOffer?.Id, myLatestAmount);
    }
}
