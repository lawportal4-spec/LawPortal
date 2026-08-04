using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Offers.Dtos;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Queries;

/// <summary>The lawyer's bidding feed — every still-open bidding request they were invited to
/// (targeted or matched by a broadcast fan-out), plus their own offer status on each so the UI
/// can show "no offer yet" vs "you offered 500 SAR" without a second call per row.</summary>
public record GetBiddingFeedQuery(int Page, int PageSize) : IRequest<PagedResult<BiddingFeedItemDto>>;

public class GetBiddingFeedHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetBiddingFeedQuery, PagedResult<BiddingFeedItemDto>>
{
    public async Task<PagedResult<BiddingFeedItemDto>> Handle(GetBiddingFeedQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query =
            from invitation in db.RequestInvitations
            join bidding in db.BiddingRequests on invitation.ServiceRequestId equals bidding.Id
            where invitation.LawyerProfileId == lawyerProfileId && bidding.Status == RequestStatus.Submitted
            orderby invitation.CreatedAtUtc descending
            select new { invitation, bidding };

        var totalCount = await query.CountAsync(cancellationToken);
        var page_ = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var items = new List<BiddingFeedItemDto>(page_.Count);
        foreach (var row in page_)
        {
            var service = await db.ServiceCatalogItems.FirstAsync(s => s.Id == row.bidding.ServiceId, cancellationToken);
            var specialty = row.bidding.SpecialtyId is { } specialtyId
                ? await db.Specialties.FirstOrDefaultAsync(s => s.Id == specialtyId, cancellationToken)
                : null;
            var myOffer = await db.Offers
                .Where(o => o.ServiceRequestId == row.bidding.Id && o.LawyerProfileId == lawyerProfileId)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            decimal? myLatestAmount = myOffer is null ? null : await db.OfferRevisions
                .Where(r => r.OfferId == myOffer.Id)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => (decimal?)r.Amount)
                .FirstOrDefaultAsync(cancellationToken);

            items.Add(new BiddingFeedItemDto(
                row.bidding.Id, row.bidding.Number, service.NameAr, service.NameEn,
                specialty?.NameAr, specialty?.NameEn, row.bidding.Title,
                row.invitation.CreatedAtUtc, myOffer?.Status.ToString(), myLatestAmount, row.bidding.CreatedAtUtc));
        }

        return new PagedResult<BiddingFeedItemDto>(items, page, pageSize, totalCount);
    }
}
