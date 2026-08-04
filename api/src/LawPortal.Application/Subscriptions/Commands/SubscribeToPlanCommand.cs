using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Payments;
using LawPortal.Application.Subscriptions.Dtos;
using LawPortal.Domain.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Subscriptions.Commands;

/// <summary>Starts a new paid subscription — never activates entitlements on credit: the
/// created row is <see cref="SubscriptionStatus.PastDue"/> with a zero-length period
/// (<c>CurrentPeriodEndUtc == CurrentPeriodStartUtc</c>) until the first invoice is actually
/// paid, so <c>SubscriptionEntitlementResolver</c> correctly falls back to the Free plan in the
/// meantime — the same "never optimistic for money" principle P4's checkout already follows.</summary>
public record SubscribeToPlanCommand(int PlanId) : IRequest<SubscriptionInvoiceDto>;

public class SubscribeToPlanValidator : AbstractValidator<SubscribeToPlanCommand>
{
    public SubscribeToPlanValidator() => RuleFor(x => x.PlanId).GreaterThan(0);
}

public class SubscribeToPlanHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SubscribeToPlanCommand, SubscriptionInvoiceDto>
{
    public async Task<SubscriptionInvoiceDto> Handle(SubscribeToPlanCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Plan not found.");
        if (plan.MonthlyPrice <= 0)
            throw new InvalidOperationException("The free plan doesn't need a subscription — it's the default.");

        var hasUnresolvedSubscription = await db.LawyerSubscriptions.AnyAsync(
            s => s.LawyerProfileId == lawyerProfileId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue),
            cancellationToken);
        if (hasUnresolvedSubscription)
            throw new InvalidOperationException("You already have a subscription in progress — cancel it or wait for it to resolve first.");

        var now = DateTime.UtcNow;
        var subscription = new LawyerSubscription
        {
            Id = Guid.NewGuid(),
            LawyerProfileId = lawyerProfileId,
            PlanId = plan.Id,
            Status = SubscriptionStatus.PastDue,
            CurrentPeriodStartUtc = now,
            CurrentPeriodEndUtc = now,
        };
        db.LawyerSubscriptions.Add(subscription);

        var invoice = await SubscriptionInvoiceFactory.CreateAsync(db, subscription, periodStartUtc: now, cancellationToken);
        db.SubscriptionInvoices.Add(invoice);

        await db.SaveChangesAsync(cancellationToken);

        return new SubscriptionInvoiceDto(
            invoice.Id, invoice.Number, invoice.PeriodStartUtc, invoice.PeriodEndUtc,
            invoice.SubtotalExVat, invoice.VatAmount, invoice.Total, invoice.Status.ToString(),
            invoice.DueAtUtc, invoice.PaidAtUtc);
    }
}

/// <summary>The one place that builds a <see cref="SubscriptionInvoice"/> — used for a brand
/// new subscription and for every renewal/dunning-retry cycle
/// (<c>SubscriptionRenewalService</c>), so the numbering and VAT math never drift between the
/// two call sites.</summary>
public static class SubscriptionInvoiceFactory
{
    public static async Task<SubscriptionInvoice> CreateAsync(
        ILawPortalDbContext db, LawyerSubscription subscription, DateTime periodStartUtc, CancellationToken cancellationToken)
    {
        var plan = subscription.Plan ?? await db.SubscriptionPlans.FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);

        var year = DateTime.UtcNow.Year;
        var countThisYear = await db.SubscriptionInvoices.CountAsync(i => i.CreatedAtUtc.Year == year, cancellationToken);
        var number = $"SUB-{year}-{(countThisYear + 1):D6}";

        // Subscription fees are stated VAT-inclusive, same convention as P2's lawyer pricing.
        var vatAmount = Math.Round(plan.MonthlyPrice * PaymentBreakdownCalculator.VatRate / (1 + PaymentBreakdownCalculator.VatRate), 2);

        return new SubscriptionInvoice
        {
            Id = Guid.NewGuid(),
            LawyerSubscriptionId = subscription.Id,
            Number = number,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodStartUtc.AddDays(30),
            SubtotalExVat = plan.MonthlyPrice - vatAmount,
            VatAmount = vatAmount,
            Total = plan.MonthlyPrice,
            DueAtUtc = DateTime.UtcNow.AddDays(3),
        };
    }
}
