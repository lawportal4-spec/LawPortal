using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Subscriptions;

/// <summary>
/// Resolves which <see cref="SubscriptionPlan"/> actually governs a lawyer's entitlements right
/// now — a paid plan only while it's genuinely still in force (<see cref="SubscriptionStatus.Active"/>,
/// or <see cref="SubscriptionStatus.Canceled"/> but not yet past the period the lawyer already
/// paid for), falling back to the cheapest active plan (the seeded Free tier) for every other
/// lawyer — including ones who never subscribed at all, so no backfill is needed. Consulted by
/// <c>CommissionPolicyResolver</c> (commission discount) and <c>BidFanOutConsumer</c> (broadcast
/// bidding eligibility) — the two entitlements a plan tier actually changes.
/// </summary>
public static class SubscriptionEntitlementResolver
{
    public static async Task<SubscriptionPlan> GetEffectivePlanAsync(ILawPortalDbContext db, Guid lawyerProfileId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var paidSubscription = await db.LawyerSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.LawyerProfileId == lawyerProfileId
                && s.CurrentPeriodEndUtc > now
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Canceled))
            .OrderByDescending(s => s.CurrentPeriodEndUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (paidSubscription?.Plan is not null) return paidSubscription.Plan;

        return await db.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPrice)
            .FirstAsync(cancellationToken);
    }
}
