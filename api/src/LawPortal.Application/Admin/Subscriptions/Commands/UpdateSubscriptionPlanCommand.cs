using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Subscriptions.Commands;

/// <summary>Edits an existing plan in place — unlike <c>CommissionPolicy</c>, a subscription
/// plan is a product listing, not a rate-history record: changing its price going forward is
/// fine (an already-issued <see cref="Domain.Subscriptions.SubscriptionInvoice"/> keeps its own
/// snapshotted amount regardless of a later price change here).</summary>
public record UpdateSubscriptionPlanCommand(
    int Id, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    decimal MonthlyPrice, decimal? CommissionPercentageOverride, bool IncludesBroadcastBidding,
    int SortOrder, bool IsActive) : IRequest<Unit>;

public class UpdateSubscriptionPlanValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CommissionPercentageOverride).InclusiveBetween(0, 100).When(x => x.CommissionPercentageOverride is not null);
    }
}

public class UpdateSubscriptionPlanHandler(ILawPortalDbContext db) : IRequestHandler<UpdateSubscriptionPlanCommand, Unit>
{
    public async Task<Unit> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Plan not found.");

        plan.NameAr = request.NameAr;
        plan.NameEn = request.NameEn;
        plan.DescriptionAr = request.DescriptionAr;
        plan.DescriptionEn = request.DescriptionEn;
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.CommissionPercentageOverride = request.CommissionPercentageOverride;
        plan.IncludesBroadcastBidding = request.IncludesBroadcastBidding;
        plan.SortOrder = request.SortOrder;
        plan.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
