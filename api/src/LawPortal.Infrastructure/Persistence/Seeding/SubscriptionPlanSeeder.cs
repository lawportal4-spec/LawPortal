using LawPortal.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the three tiers — real, placeholder numbers pending an actual pricing decision, same
/// shape as <see cref="CommissionPolicySeeder"/>'s own 15% global rate. Free is the implicit
/// default every lawyer starts on (see <c>SubscriptionEntitlementResolver</c>); Pro and Premium
/// trade a monthly fee for a lower marketplace commission and eligibility for broadcast bidding
/// leads — the two entitlements this pass actually wires up. Idempotent.
/// </summary>
public static class SubscriptionPlanSeeder
{
    public static async Task SeedAsync(LawPortalDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.SubscriptionPlans.AnyAsync(cancellationToken)) return;

        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Slug = "free",
                NameAr = "مجاني",
                NameEn = "Free",
                DescriptionAr = "ابدأ بدون رسوم — استقبل الاستشارات وقدّم عروضًا على الطلبات المُرسلة لك بالاسم.",
                DescriptionEn = "Start at no cost — take consultations and bid on requests sent to you by name.",
                MonthlyPrice = 0,
                CommissionPercentageOverride = null,
                IncludesBroadcastBidding = false,
                SortOrder = 1,
            },
            new SubscriptionPlan
            {
                Slug = "pro",
                NameAr = "برو",
                NameEn = "Pro",
                DescriptionAr = "عمولة مخفّضة، بالإضافة إلى عروض المزايدة العامة لكل طلبات القضاء والتنفيذ في تخصصك.",
                DescriptionEn = "A lower commission rate, plus broadcast bidding leads for every judiciary & execution request in your specialty.",
                MonthlyPrice = 149.00m,
                CommissionPercentageOverride = 10.00m,
                IncludesBroadcastBidding = true,
                SortOrder = 2,
            },
            new SubscriptionPlan
            {
                Slug = "premium",
                NameAr = "بريميوم",
                NameEn = "Premium",
                DescriptionAr = "أدنى عمولة على الإطلاق، مع نفس أولوية عروض المزايدة العامة الخاصة بخطة برو.",
                DescriptionEn = "The lowest commission rate available, with the same broadcast bidding priority as Pro.",
                MonthlyPrice = 349.00m,
                CommissionPercentageOverride = 7.00m,
                IncludesBroadcastBidding = true,
                SortOrder = 3,
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
