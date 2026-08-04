using LawPortal.Domain.Common;

namespace LawPortal.Domain.Subscriptions;

/// <summary>
/// A tier a lawyer can subscribe to — configurable policy data, never a constant, same
/// philosophy as <see cref="Billing.CommissionPolicy"/>. Every lawyer without an
/// <see cref="LawyerSubscription"/> row is implicitly on whichever plan has the lowest
/// <see cref="MonthlyPrice"/> (seeded as "Free"), resolved lazily by
/// <c>SubscriptionEntitlementResolver</c> rather than backfilled onto every lawyer profile —
/// the same lazy-creation shape P4's wallet already uses.
///
/// The two entitlements a tier actually changes: <see cref="CommissionPercentageOverride"/>
/// (null falls back to the existing category/global <see cref="Billing.CommissionPolicy"/>) and
/// <see cref="IncludesBroadcastBidding"/> (gates whether <c>BidFanOutConsumer</c> will ever match
/// this lawyer to a broadcast bidding request — a Free-tier lawyer can still be hand-picked via
/// a targeted send, and can still take consultations normally; they just don't get inbound
/// broadcast leads without paying for them).
/// </summary>
public class SubscriptionPlan : Entity<int>
{
    public required string Slug { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    public decimal MonthlyPrice { get; set; }
    public decimal? CommissionPercentageOverride { get; set; }
    public bool IncludesBroadcastBidding { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
