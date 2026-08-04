using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments;

public static class CommissionPolicyResolver
{
    /// <summary>A subscribed lawyer's plan-level discount (see <c>SubscriptionEntitlementResolver</c>)
    /// takes precedence over category/global policy when <paramref name="lawyerProfileId"/> is
    /// known — a P10 lawyer paying for a cheaper commission rate would otherwise never see it
    /// applied. <paramref name="lawyerProfileId"/> is null for a <c>CatalogRequest</c>, which has
    /// no assigned lawyer to discount for at checkout time.</summary>
    public static async Task<decimal> ResolvePercentageAsync(
        ILawPortalDbContext db, string? categorySlug, Guid? lawyerProfileId, CancellationToken cancellationToken)
    {
        if (lawyerProfileId is { } id)
        {
            var plan = await SubscriptionEntitlementResolver.GetEffectivePlanAsync(db, id, cancellationToken);
            if (plan.CommissionPercentageOverride is { } overridePercentage) return overridePercentage;
        }

        var now = DateTime.UtcNow;

        if (categorySlug is not null)
        {
            var specific = await db.CommissionPolicies
                .Where(p => p.IsActive && p.ServiceCategorySlug == categorySlug && p.EffectiveFromUtc <= now)
                .OrderByDescending(p => p.EffectiveFromUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (specific is not null) return specific.Percentage;
        }

        var global = await db.CommissionPolicies
            .Where(p => p.IsActive && p.ServiceCategorySlug == null && p.EffectiveFromUtc <= now)
            .OrderByDescending(p => p.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active commission policy is configured.");

        return global.Percentage;
    }
}
