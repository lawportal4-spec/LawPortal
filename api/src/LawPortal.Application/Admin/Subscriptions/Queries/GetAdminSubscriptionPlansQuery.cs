using LawPortal.Application.Admin.Subscriptions.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Subscriptions.Queries;

/// <summary>Unlike the lawyer-facing <c>GetSubscriptionPlansQuery</c>, this includes inactive
/// (retired) plans too — an admin needs to see what's hidden, not just what's sellable — plus
/// how many lawyers are actually subscribed to each, which the lawyer's own view has no reason
/// to know.</summary>
public record GetAdminSubscriptionPlansQuery : IRequest<IReadOnlyList<AdminSubscriptionPlanDto>>;

public class GetAdminSubscriptionPlansHandler(ILawPortalDbContext db)
    : IRequestHandler<GetAdminSubscriptionPlansQuery, IReadOnlyList<AdminSubscriptionPlanDto>>
{
    public async Task<IReadOnlyList<AdminSubscriptionPlanDto>> Handle(GetAdminSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var plans = await db.SubscriptionPlans.OrderBy(p => p.SortOrder).ToListAsync(cancellationToken);

        var activeCounts = await db.LawyerSubscriptions
            .Where(s => s.CurrentPeriodEndUtc > now && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Canceled))
            .GroupBy(s => s.PlanId)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.PlanId, g => g.Count, cancellationToken);

        return plans.Select(p => new AdminSubscriptionPlanDto(
            p.Id, p.Slug, p.NameAr, p.NameEn, p.DescriptionAr, p.DescriptionEn,
            p.MonthlyPrice, p.CommissionPercentageOverride, p.IncludesBroadcastBidding,
            p.SortOrder, p.IsActive, activeCounts.GetValueOrDefault(p.Id))).ToList();
    }
}
