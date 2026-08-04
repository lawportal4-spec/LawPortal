using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

public record UpdateLawyerPricingCommand(decimal WrittenPrice, decimal Price15, decimal Price30, decimal Price45) : IRequest<Unit>;

public class UpdateLawyerPricingValidator : AbstractValidator<UpdateLawyerPricingCommand>
{
    public UpdateLawyerPricingValidator()
    {
        RuleFor(x => x.WrittenPrice).GreaterThan(0);
        RuleFor(x => x.Price15).GreaterThan(0);
        RuleFor(x => x.Price30).GreaterThan(0);
        RuleFor(x => x.Price45).GreaterThan(0);
    }
}

public class UpdateLawyerPricingHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateLawyerPricingCommand, Unit>
{
    public async Task<Unit> Handle(UpdateLawyerPricingCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var pricing = await db.LawyerPricings.FirstOrDefaultAsync(p => p.LawyerProfileId == lawyerProfileId, cancellationToken);
        if (pricing is null)
        {
            pricing = new LawyerPricing { Id = Guid.NewGuid(), LawyerProfileId = lawyerProfileId };
            db.LawyerPricings.Add(pricing);
        }

        pricing.WrittenPrice = request.WrittenPrice;
        pricing.Price15 = request.Price15;
        pricing.Price30 = request.Price30;
        pricing.Price45 = request.Price45;
        pricing.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
