using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Requests.Commands;
using LawPortal.Application.Requests.Dtos;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Queries;

public record GetMyRequestsQuery(
    RequestStatus? Status,
    string? Kind,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize) : IRequest<PagedResult<RequestSummaryDto>>;

public class GetMyRequestsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyRequestsQuery, PagedResult<RequestSummaryDto>>
{
    public async Task<PagedResult<RequestSummaryDto>> Handle(GetMyRequestsQuery request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = db.ServiceRequests.Where(r => r.ClientId == clientId);

        if (request.Status is { } status) query = query.Where(r => r.Status == status);
        if (request.From is { } from) query = query.Where(r => r.CreatedAtUtc >= from);
        if (request.To is { } to) query = query.Where(r => r.CreatedAtUtc <= to);
        if (request.Kind == "Consultation") query = query.OfType<ConsultationRequest>();
        if (request.Kind == "Catalog") query = query.OfType<CatalogRequest>();
        if (request.Kind == "Bidding") query = query.OfType<BiddingRequest>();

        query = query.OrderByDescending(r => r.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var page_ = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Service)
            .ToListAsync(cancellationToken);

        var items = page_.Select(r => new RequestSummaryDto(
            r.Id,
            r.Number,
            r switch { ConsultationRequest => "Consultation", BiddingRequest => "Bidding", _ => "Catalog" },
            r.Service!.NameAr,
            r.Service.NameEn,
            r.Status.ToString(),
            r.Title,
            r.Subtotal,
            r.CreatedAtUtc)).ToList();

        return new PagedResult<RequestSummaryDto>(items, page, pageSize, totalCount);
    }
}
