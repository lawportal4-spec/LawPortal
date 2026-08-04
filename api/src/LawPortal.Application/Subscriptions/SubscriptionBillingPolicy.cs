namespace LawPortal.Application.Subscriptions;

/// <summary>Shared dunning constants — read by both the payment webhook (which counts a failed
/// or expired-unpaid invoice against the budget) and <c>SubscriptionRenewalService</c> (which
/// spends the budget by generating retry invoices), so the two can never disagree about when a
/// subscription is actually out of chances.</summary>
public static class SubscriptionBillingPolicy
{
    public const int MaxDunningAttempts = 3;
    public const int RetryGraceDays = 3;
}
