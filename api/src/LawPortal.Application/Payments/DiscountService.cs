using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using LawPortal.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments;

/// <summary>Why a code can't be used. The name goes to the client as-is and is translated there
/// (<c>discount.reasons.*</c>).</summary>
public enum DiscountRejection
{
    NotFound,
    Inactive,
    NotStarted,
    Expired,
    WrongScope,
    UsageLimitReached,
    PerUserLimitReached,
    BelowMinimum,
    NotFirstPayment,
}

public record DiscountEvaluation(DiscountCode? Code, decimal Amount, DiscountRejection? Rejection)
{
    public bool IsValid => Rejection is null;
}

/// <summary>
/// Discount codes across every payable path. A checkout <see cref="ReserveAsync">reserves</see> a
/// Pending redemption so limits hold while the client is at the gateway; the webhook
/// <see cref="ConfirmAsync">confirms</see> it on success and <see cref="ReleaseAsync">releases</see>
/// it on failure. A Pending row nobody settled (an abandoned card page) stops counting after
/// <see cref="PendingHold"/>.
/// </summary>
public static class DiscountService
{
    public static readonly TimeSpan PendingHold = TimeSpan.FromMinutes(30);

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public static DiscountScope ScopeOf(ServiceRequest request) => request switch
    {
        ConsultationRequest { ConsultationType: ConsultationType.Instant } => DiscountScope.InstantConsultation,
        ConsultationRequest { ConsultationType: ConsultationType.Scheduled } => DiscountScope.ScheduledConsultation,
        ConsultationRequest => DiscountScope.WrittenConsultation,
        BiddingRequest => DiscountScope.BiddingRequest,
        _ => DiscountScope.CatalogService,
    };

    /// <summary>The discount <paramref name="code"/> gives on a VAT-inclusive <paramref name="gross"/>.</summary>
    public static decimal ComputeAmount(DiscountCode code, decimal gross)
    {
        var amount = code.Kind == DiscountKind.Percentage
            ? Math.Round(gross * code.Value / 100m, 2)
            : code.Value;
        if (code.MaxDiscountAmount is { } cap) amount = Math.Min(amount, cap);
        // May cover the whole price: a zero total settles without the gateway (see the pay commands).
        return Math.Max(0, Math.Min(amount, gross));
    }

    public static async Task<DiscountEvaluation> EvaluateAsync(
        ILawPortalDbContext db, string rawCode, DiscountScope scope, Guid userId, decimal gross, CancellationToken cancellationToken)
    {
        var normalized = Normalize(rawCode);
        var code = await db.DiscountCodes.FirstOrDefaultAsync(c => c.Code == normalized, cancellationToken);
        if (code is null) return Reject(null, DiscountRejection.NotFound);

        var now = DateTime.UtcNow;
        if (!code.IsActive) return Reject(code, DiscountRejection.Inactive);
        if (code.StartsAtUtc > now) return Reject(code, DiscountRejection.NotStarted);
        if (code.EndsAtUtc <= now) return Reject(code, DiscountRejection.Expired);
        if ((code.Scopes & scope) == 0) return Reject(code, DiscountRejection.WrongScope);
        if (code.MinAmount is { } min && gross < min) return Reject(code, DiscountRejection.BelowMinimum);

        // The user's own unsettled attempts don't count: ReserveAsync releases them before reserving again.
        var holdCutoff = now - PendingHold;
        var counted = db.DiscountRedemptions.Where(r => r.DiscountCodeId == code.Id
            && (r.Status == DiscountRedemptionStatus.Confirmed
                || (r.Status == DiscountRedemptionStatus.Pending && r.CreatedAtUtc > holdCutoff && r.UserId != userId)));
        if (code.UsageLimit is { } limit && await counted.CountAsync(cancellationToken) >= limit)
            return Reject(code, DiscountRejection.UsageLimitReached);
        if (code.PerUserLimit is { } perUser && await counted.CountAsync(r => r.UserId == userId, cancellationToken) >= perUser)
            return Reject(code, DiscountRejection.PerUserLimitReached);

        if (code.FirstPaymentOnly && await HasPaidBeforeAsync(db, userId, cancellationToken))
            return Reject(code, DiscountRejection.NotFirstPayment);

        return new DiscountEvaluation(code, ComputeAmount(code, gross), null);
    }

    /// <summary>Evaluates and, when valid, adds a Pending redemption against <paramref name="referenceId"/>
    /// (the payment or invoice id). Throws with the rejection name when the code can't be used.</summary>
    public static async Task<DiscountEvaluation> ReserveAsync(
        ILawPortalDbContext db, string rawCode, DiscountScope scope, Guid userId, decimal gross, Guid referenceId, CancellationToken cancellationToken)
    {
        var evaluation = await EvaluateAsync(db, rawCode, scope, userId, gross, cancellationToken);
        if (!evaluation.IsValid)
            throw new InvalidOperationException($"Discount code rejected: {evaluation.Rejection}.");

        // ponytail: no row lock, so two simultaneous checkouts can both take the last use; add one if codes get contested.
        var stale = await db.DiscountRedemptions
            .Where(r => r.DiscountCodeId == evaluation.Code!.Id && r.UserId == userId && r.Status == DiscountRedemptionStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var r in stale) r.Status = DiscountRedemptionStatus.Released;

        db.DiscountRedemptions.Add(new DiscountRedemption
        {
            Id = Guid.NewGuid(),
            DiscountCodeId = evaluation.Code!.Id,
            UserId = userId,
            Scope = scope,
            ReferenceId = referenceId,
            Amount = evaluation.Amount,
        });
        return evaluation;
    }

    public static Task ConfirmAsync(ILawPortalDbContext db, Guid referenceId, CancellationToken cancellationToken) =>
        SettleAsync(db, referenceId, DiscountRedemptionStatus.Confirmed, cancellationToken);

    public static Task ReleaseAsync(ILawPortalDbContext db, Guid referenceId, CancellationToken cancellationToken) =>
        SettleAsync(db, referenceId, DiscountRedemptionStatus.Released, cancellationToken);

    private static async Task SettleAsync(ILawPortalDbContext db, Guid referenceId, DiscountRedemptionStatus status, CancellationToken cancellationToken)
    {
        // Also looks at Released rows: a payment can land after the user's retry released its hold,
        // and money taken with a discount must count as a use.
        // Local first: a wallet checkout reserves and confirms within one SaveChanges.
        var redemption = db.DiscountRedemptions.Local.FirstOrDefault(r => r.ReferenceId == referenceId && r.Status != DiscountRedemptionStatus.Confirmed)
            ?? await db.DiscountRedemptions
                .FirstOrDefaultAsync(r => r.ReferenceId == referenceId && r.Status != DiscountRedemptionStatus.Confirmed, cancellationToken);
        if (redemption is not null && (status == DiscountRedemptionStatus.Confirmed || redemption.Status == DiscountRedemptionStatus.Pending))
            redemption.Status = status;
    }

    /// <summary>Any earlier successful payment by this user, as a client or as a lawyer.</summary>
    private static async Task<bool> HasPaidBeforeAsync(ILawPortalDbContext db, Guid userId, CancellationToken cancellationToken) =>
        await db.Payments.AnyAsync(p => p.Status == PaymentStatus.Paid && p.Purpose == PaymentPurpose.RequestCheckout
                && db.ClientProfiles.Any(c => c.Id == p.ClientId && c.UserId == userId), cancellationToken)
        || await db.SubscriptionInvoices.AnyAsync(i => i.Status == SubscriptionInvoiceStatus.Paid
                && i.LawyerSubscription!.LawyerProfile!.UserId == userId, cancellationToken)
        || await db.RegistrationFeeInvoices.AnyAsync(i => i.Status == RegistrationFeeInvoiceStatus.Paid
                && db.LawyerProfiles.Any(l => l.Id == i.LawyerProfileId && l.UserId == userId), cancellationToken);

    private static DiscountEvaluation Reject(DiscountCode? code, DiscountRejection rejection) => new(code, 0, rejection);
}
