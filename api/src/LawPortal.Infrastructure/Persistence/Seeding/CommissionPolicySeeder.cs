using LawPortal.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Infrastructure.Persistence.Seeding;

/// <summary>Seeds the single global commission rate this pass actually resolves against —
/// category-specific overrides are modeled but unused until real per-category rates are decided
/// (an open question the plan itself flags: "launch needs real numbers"). Idempotent.</summary>
public static class CommissionPolicySeeder
{
    public const decimal DefaultPercentage = 15.00m;

    public static async Task SeedAsync(LawPortalDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.CommissionPolicies.AnyAsync(cancellationToken)) return;

        db.CommissionPolicies.Add(new CommissionPolicy
        {
            ServiceCategorySlug = null,
            Percentage = DefaultPercentage,
            IsActive = true,
            EffectiveFromUtc = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
