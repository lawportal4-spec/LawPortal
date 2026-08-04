using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

public record GetLedgerEntriesQuery(int Page, int PageSize) : IRequest<PagedResult<LedgerEntryDto>>;

public class GetLedgerEntriesHandler(ILawPortalDbContext db) : IRequestHandler<GetLedgerEntriesQuery, PagedResult<LedgerEntryDto>>
{
    public async Task<PagedResult<LedgerEntryDto>> Handle(GetLedgerEntriesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.LedgerEntries.OrderByDescending(e => e.CreatedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new LedgerEntryDto(e.Id, e.Account.ToString(), e.IsDebit, e.Amount, e.ReferenceType, e.ReferenceId, e.Description, e.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerEntryDto>(items, page, pageSize, totalCount);
    }
}
