using LawPortal.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests;

/// <summary>Human-facing request numbers, e.g. LP-2026-000123. A DB-backed sequence with
/// proper concurrency guarantees is a fine later refinement; this pass accepts the small race
/// window in exchange for not needing new infrastructure to submit a request.</summary>
public static class RequestNumberGenerator
{
    public static async Task<string> NextAsync(ILawPortalDbContext db, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var countThisYear = await db.ServiceRequests.CountAsync(r => r.CreatedAtUtc.Year == year, cancellationToken);
        return $"LP-{year}-{(countThisYear + 1):D6}";
    }
}
