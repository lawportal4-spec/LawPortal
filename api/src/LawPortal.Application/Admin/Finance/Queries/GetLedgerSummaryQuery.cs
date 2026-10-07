using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

/// <summary>The reconciliation view — the same "sum every debit, sum every credit, they must
/// match" check this project has run by hand via curl+SQL after every money-moving action since
/// P4. <see cref="LedgerSummaryDto.IsBalanced"/> being false would mean the double-entry
/// invariant has broken somewhere; every phase's own verification has confirmed it hasn't.</summary>
/// <remarks>Optional <c>From</c>/<c>To</c> limit it to the movements in that period.</remarks>
public record GetLedgerSummaryQuery(DateTime? From = null, DateTime? To = null) : IRequest<LedgerSummaryDto>;

public class GetLedgerSummaryHandler(ILawPortalDbContext db) : IRequestHandler<GetLedgerSummaryQuery, LedgerSummaryDto>
{
    public async Task<LedgerSummaryDto> Handle(GetLedgerSummaryQuery request, CancellationToken cancellationToken)
    {
        var entries = db.LedgerEntries.AsQueryable();
        if (request.From is { } from) entries = entries.Where(e => e.CreatedAtUtc >= from);
        if (request.To is { } to) entries = entries.Where(e => e.CreatedAtUtc <= to);

        var grouped = await entries
            .GroupBy(e => e.Account)
            .Select(g => new
            {
                Account = g.Key,
                TotalDebits = g.Where(e => e.IsDebit).Sum(e => e.Amount),
                TotalCredits = g.Where(e => !e.IsDebit).Sum(e => e.Amount),
            })
            .ToListAsync(cancellationToken);

        var accounts = grouped
            .OrderBy(g => g.Account)
            .Select(g => new LedgerAccountBalanceDto(g.Account.ToString(), g.TotalDebits, g.TotalCredits, g.TotalDebits - g.TotalCredits))
            .ToList();

        var grandDebits = accounts.Sum(a => a.TotalDebits);
        var grandCredits = accounts.Sum(a => a.TotalCredits);

        return new LedgerSummaryDto(accounts, grandDebits, grandCredits, grandDebits == grandCredits);
    }
}
