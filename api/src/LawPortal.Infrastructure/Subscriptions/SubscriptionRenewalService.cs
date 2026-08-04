using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Subscriptions;
using LawPortal.Application.Subscriptions.Commands;
using LawPortal.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LawPortal.Infrastructure.Subscriptions;

/// <summary>
/// The only thing that creates a renewal or dunning-retry <see cref="SubscriptionInvoice"/> —
/// the payment webhook only ever records an outcome, never generates the next invoice itself,
/// so the two can't race or double-create one. Never auto-charges: there is no stored payment
/// method in this codebase's model, so a "renewal" just means "the next invoice exists and is
/// waiting to be paid," exactly like a brand-new subscription's first invoice.
///
/// Polls every <see cref="PollInterval"/> — a genuinely short interval chosen so a 30-day
/// billing cycle and a 3-day dunning window are actually observable within a single dev/test
/// session. A real deployment would run this once a day via a proper scheduler, not a
/// short-interval background poll; stated explicitly rather than left implicit.
/// </summary>
public class SubscriptionRenewalService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionRenewalService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ILawPortalDbContext>();
                await RunSweepAsync(db, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Subscription renewal sweep failed; will retry next tick.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }

    private static async Task RunSweepAsync(ILawPortalDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Active subscriptions whose paid period just ended.
        var lapsedActive = await db.LawyerSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEndUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var subscription in lapsedActive)
        {
            if (subscription.CancelAtPeriodEnd)
            {
                subscription.Status = SubscriptionStatus.Canceled;
                continue;
            }

            var renewalInvoice = await SubscriptionInvoiceFactory.CreateAsync(db, subscription, periodStartUtc: subscription.CurrentPeriodEndUtc, cancellationToken);
            db.SubscriptionInvoices.Add(renewalInvoice);
            subscription.Status = SubscriptionStatus.PastDue;
        }

        // PastDue subscriptions whose current pending invoice's due date has passed without
        // ever being paid — that's a failure signal just as real as an explicit gateway
        // "failed" callback, so it counts against the same dunning budget.
        var pastDue = await db.LawyerSubscriptions
            .Where(s => s.Status == SubscriptionStatus.PastDue)
            .ToListAsync(cancellationToken);

        foreach (var subscription in pastDue)
        {
            var pendingInvoice = await db.SubscriptionInvoices
                .Where(i => i.LawyerSubscriptionId == subscription.Id && i.Status == SubscriptionInvoiceStatus.Pending)
                .OrderByDescending(i => i.DueAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (pendingInvoice is null)
            {
                // Shouldn't normally happen (PastDue always implies a live pending invoice),
                // but self-heal rather than leave the subscription stuck with no path forward.
                var retryInvoice = await SubscriptionInvoiceFactory.CreateAsync(db, subscription, periodStartUtc: now, cancellationToken);
                db.SubscriptionInvoices.Add(retryInvoice);
                continue;
            }

            if (pendingInvoice.DueAtUtc > now) continue; // still within the payment window

            pendingInvoice.Status = SubscriptionInvoiceStatus.Failed;
            pendingInvoice.FailureReason = "Payment window expired with no successful charge.";
            subscription.ConsecutiveFailedAttempts++;

            if (subscription.ConsecutiveFailedAttempts >= SubscriptionBillingPolicy.MaxDunningAttempts)
            {
                subscription.Status = SubscriptionStatus.Expired;
            }
            else
            {
                var retryInvoice = await SubscriptionInvoiceFactory.CreateAsync(db, subscription, periodStartUtc: pendingInvoice.PeriodStartUtc, cancellationToken);
                retryInvoice.PeriodEndUtc = pendingInvoice.PeriodEndUtc;
                retryInvoice.DueAtUtc = now.AddDays(SubscriptionBillingPolicy.RetryGraceDays);
                db.SubscriptionInvoices.Add(retryInvoice);
            }
        }

        if (lapsedActive.Count > 0 || pastDue.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
