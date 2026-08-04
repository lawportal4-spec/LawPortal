using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

public record GetIncomingRequestsQuery(RequestStatus? Status, int Page, int PageSize) : IRequest<PagedResult<LawyerRequestSummaryDto>>;

public class GetIncomingRequestsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetIncomingRequestsQuery, PagedResult<LawyerRequestSummaryDto>>
{
    public async Task<PagedResult<LawyerRequestSummaryDto>> Handle(GetIncomingRequestsQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = db.ConsultationRequests.Where(c => c.LawyerProfileId == lawyerProfileId);
        if (request.Status is { } status) query = query.Where(c => c.Status == status);
        query = query.OrderByDescending(c => c.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var page_ = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Service)
            .Include(c => c.Client)
            .ToListAsync(cancellationToken);

        var items = page_.Select(c => new LawyerRequestSummaryDto(
            c.Id, c.Number, c.Client?.FullName ?? "—", c.Service!.NameAr, c.Service.NameEn,
            c.Status.ToString(), c.Title, c.Subtotal, c.CreatedAtUtc)).ToList();

        return new PagedResult<LawyerRequestSummaryDto>(items, page, pageSize, totalCount);
    }
}
