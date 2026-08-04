using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Subscriptions.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Subscriptions.Queries;

public record GetSubscriptionPlansQuery : IRequest<IReadOnlyList<SubscriptionPlanDto>>;

public class GetSubscriptionPlansHandler(ILawPortalDbContext db) : IRequestHandler<GetSubscriptionPlansQuery, IReadOnlyList<SubscriptionPlanDto>>
{
    public async Task<IReadOnlyList<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken) =>
        await db.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new SubscriptionPlanDto(
                p.Id, p.Slug, p.NameAr, p.NameEn, p.DescriptionAr, p.DescriptionEn,
                p.MonthlyPrice, p.CommissionPercentageOverride, p.IncludesBroadcastBidding))
            .ToListAsync(cancellationToken);
}
