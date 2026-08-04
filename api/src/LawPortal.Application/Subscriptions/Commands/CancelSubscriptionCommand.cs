using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Domain.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Subscriptions.Commands;

/// <summary>Cancels at period end, not immediately — the lawyer already paid for the current
/// period and keeps its entitlements until <see cref="LawyerSubscription.CurrentPeriodEndUtc"/>;
/// <c>SubscriptionRenewalService</c> simply won't generate a renewal invoice once that arrives.</summary>
public record CancelSubscriptionCommand : IRequest<Unit>;

public class CancelSubscriptionHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<CancelSubscriptionCommand, Unit>
{
    public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var subscription = await db.LawyerSubscriptions
            .Where(s => s.LawyerProfileId == lawyerProfileId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue))
            .OrderByDescending(s => s.CurrentPeriodEndUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("You have no active subscription to cancel.");

        subscription.CancelAtPeriodEnd = true;
        if (subscription.Status == SubscriptionStatus.PastDue)
        {
            // Nothing paid yet on this cycle — cancelling now means it never activates at all.
            subscription.Status = SubscriptionStatus.Canceled;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
