using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Subscriptions.Dtos;
using LawPortal.Domain.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Subscriptions.Queries;

public record GetMySubscriptionQuery : IRequest<MySubscriptionDto>;

public class GetMySubscriptionHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMySubscriptionQuery, MySubscriptionDto>
{
    public async Task<MySubscriptionDto> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var effectivePlan = await SubscriptionEntitlementResolver.GetEffectivePlanAsync(db, lawyerProfileId, cancellationToken);

        // The lawyer's most recent subscription row, if any — shown even once it's no longer
        // the one granting entitlements (e.g. Expired), so the settings page can explain why.
        var subscription = await db.LawyerSubscriptions
            .Where(s => s.LawyerProfileId == lawyerProfileId)
            .OrderByDescending(s => s.CurrentPeriodEndUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var planDto = new SubscriptionPlanDto(
            effectivePlan.Id, effectivePlan.Slug, effectivePlan.NameAr, effectivePlan.NameEn,
            effectivePlan.DescriptionAr, effectivePlan.DescriptionEn, effectivePlan.MonthlyPrice,
            effectivePlan.CommissionPercentageOverride, effectivePlan.IncludesBroadcastBidding);

        return new MySubscriptionDto(
            planDto,
            subscription?.Id,
            subscription?.Status.ToString(),
            subscription?.CurrentPeriodStartUtc,
            subscription?.CurrentPeriodEndUtc,
            subscription?.CancelAtPeriodEnd ?? false);
    }
}
