using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

public record GetCommissionPoliciesQuery : IRequest<IReadOnlyList<CommissionPolicyDto>>;

public class GetCommissionPoliciesHandler(ILawPortalDbContext db) : IRequestHandler<GetCommissionPoliciesQuery, IReadOnlyList<CommissionPolicyDto>>
{
    public async Task<IReadOnlyList<CommissionPolicyDto>> Handle(GetCommissionPoliciesQuery request, CancellationToken cancellationToken)
    {
        var all = await db.CommissionPolicies.OrderByDescending(p => p.EffectiveFromUtc).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        // Same pick as CommissionPolicyResolver: per category, the newest active one already in effect.
        var applied = all.Where(p => p.IsActive && p.EffectiveFromUtc <= now)
            .GroupBy(p => p.ServiceCategorySlug)
            .Select(g => g.First().Id)
            .ToHashSet();

        return all.Select(p => new CommissionPolicyDto(p.Id, p.ServiceCategorySlug, p.Percentage, p.IsActive, p.EffectiveFromUtc,
                applied.Contains(p.Id) ? "Applied" : p.IsActive && p.EffectiveFromUtc > now ? "Scheduled" : "Stopped"))
            .ToList();
    }
}
