using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments;

/// <summary>A lawyer's debt to the platform: what it is now, and how a payout pays it down.</summary>
public static class LawyerDebts
{
    public static async Task<decimal> BalanceAsync(ILawPortalDbContext db, Guid lawyerProfileId, CancellationToken cancellationToken) =>
        await db.LawyerDebtEntries.Where(e => e.LawyerProfileId == lawyerProfileId).SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

    /// <summary>The only way a payout is released: deducts any debt the lawyer owes first (they're paid
    /// Amount − DebtOffset), records the deduction, and posts the balanced ledger entries.</summary>
    public static async Task ReleasePayoutAsync(ILawPortalDbContext db, Payout payout, CancellationToken cancellationToken)
    {
        var owed = await BalanceAsync(db, payout.LawyerProfileId, cancellationToken);
        payout.DebtOffset = Math.Clamp(owed, 0m, payout.Amount);
        payout.Status = PayoutStatus.Released;
        payout.ReleasedAtUtc = DateTime.UtcNow;

        if (payout.DebtOffset > 0)
        {
            db.LawyerDebtEntries.Add(new LawyerDebtEntry
            {
                Id = Guid.NewGuid(),
                LawyerProfileId = payout.LawyerProfileId,
                Kind = LawyerDebtEntryKind.PayoutOffset,
                Amount = -payout.DebtOffset,
                PaymentId = payout.PaymentId,
                PayoutId = payout.Id,
            });
        }
        db.LedgerEntries.AddRange(LedgerPostingService.PostPayout(payout));
    }
}

public static class RefundPolicy
{
    public static async Task<RefundPolicySetting> GetAsync(ILawPortalDbContext db, CancellationToken cancellationToken) =>
        await db.RefundPolicySettings.FirstOrDefaultAsync(s => s.Id == RefundPolicySetting.SingletonId, cancellationToken)
        ?? new RefundPolicySetting { Id = RefundPolicySetting.SingletonId };

    /// <summary>Last moment a released payout's payment can still be refunded.</summary>
    public static DateTime DeadlineFor(Payout payout, RefundPolicySetting policy) =>
        (payout.ReleasedAtUtc ?? DateTime.UtcNow).AddDays(policy.RefundWindowDays);
}
