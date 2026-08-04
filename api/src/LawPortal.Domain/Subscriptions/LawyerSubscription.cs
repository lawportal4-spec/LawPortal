using LawPortal.Domain.Common;
using LawPortal.Domain.Identity;

namespace LawPortal.Domain.Subscriptions;

/// <summary>
/// <see cref="Active"/> — current period paid, entitlements apply.
/// <see cref="PastDue"/> — a renewal (or dunning-retry) invoice is outstanding; entitlements are
/// suspended immediately (fail closed, same "never optimistic for money" principle P4's checkout
/// already follows) rather than granted on credit until the retry window closes.
/// <see cref="Canceled"/> — the lawyer canceled; they keep what they already paid for until
/// <see cref="LawyerSubscription.CurrentPeriodEndUtc"/>, then no renewal invoice is generated.
/// <see cref="Expired"/> — terminal; dunning exhausted its retry budget with no successful
/// payment. A lawyer here has no live entitlements and would need to subscribe fresh.
/// </summary>
public enum SubscriptionStatus
{
    Active = 1,
    PastDue = 2,
    Canceled = 3,
    Expired = 4,
}

/// <summary>
/// One lawyer's subscription to one plan over time — only paid (non-Free) subscriptions get a
/// row here; see <see cref="SubscriptionPlan"/>'s own docs for why. Recurring billing
/// (<c>SubscriptionRenewalService</c>) never auto-charges a stored card — no such thing exists
/// in this codebase's payment model — it generates the next <see cref="SubscriptionInvoice"/>
/// and waits for the lawyer to pay it, exactly like every other payment here.
/// </summary>
public class LawyerSubscription : AggregateRoot<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public int PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>Dunning counter — reset to 0 on any successful payment. Reaching
    /// <c>SubscriptionRenewalService.MaxDunningAttempts</c> moves the subscription to
    /// <see cref="SubscriptionStatus.Expired"/>.</summary>
    public int ConsecutiveFailedAttempts { get; set; }

    public ICollection<SubscriptionInvoice> Invoices { get; set; } = [];
}
