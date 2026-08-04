using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

/// <summary>Bidding requests this lawyer actually won (an accepted offer, now Paid or
/// Completed) — the bidding-side equivalent of <see cref="GetIncomingRequestsQuery"/>, which
/// stays consultation-only since bidding requests never go through an accept/decline step (the
/// lawyer already committed by having their offer accepted).</summary>
public record GetAwardedBiddingRequestsQuery(RequestStatus? Status, int Page, int PageSize) : IRequest<PagedResult<LawyerRequestSummaryDto>>;

public class GetAwardedBiddingRequestsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetAwardedBiddingRequestsQuery, PagedResult<LawyerRequestSummaryDto>>
{
    public async Task<PagedResult<LawyerRequestSummaryDto>> Handle(GetAwardedBiddingRequestsQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = db.BiddingRequests.Where(b => b.AwardedLawyerProfileId == lawyerProfileId);
        if (request.Status is { } status) query = query.Where(b => b.Status == status);
        query = query.OrderByDescending(b => b.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var page_ = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.Service)
            .Include(b => b.Client)
            .ToListAsync(cancellationToken);

        var items = page_.Select(b => new LawyerRequestSummaryDto(
            b.Id, b.Number, b.Client?.FullName ?? "—", b.Service!.NameAr, b.Service.NameEn,
            b.Status.ToString(), b.Title, b.Subtotal, b.CreatedAtUtc)).ToList();

        return new PagedResult<LawyerRequestSummaryDto>(items, page, pageSize, totalCount);
    }
}
