using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

public record GetCommissionPoliciesQuery : IRequest<IReadOnlyList<CommissionPolicyDto>>;

public class GetCommissionPoliciesHandler(ILawPortalDbContext db) : IRequestHandler<GetCommissionPoliciesQuery, IReadOnlyList<CommissionPolicyDto>>
{
    public async Task<IReadOnlyList<CommissionPolicyDto>> Handle(GetCommissionPoliciesQuery request, CancellationToken cancellationToken) =>
        await db.CommissionPolicies
            .OrderByDescending(p => p.EffectiveFromUtc)
            .Select(p => new CommissionPolicyDto(p.Id, p.ServiceCategorySlug, p.Percentage, p.IsActive, p.EffectiveFromUtc))
            .ToListAsync(cancellationToken);
}
