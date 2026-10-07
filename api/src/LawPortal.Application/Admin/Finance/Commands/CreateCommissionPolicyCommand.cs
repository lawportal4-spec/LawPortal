using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Billing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Commands;

/// <summary>Adds a new policy row — never edits one in place, matching
/// <see cref="CommissionPolicy"/>'s own doc comment ("a rate change is a new row, not a
/// redeploy"). A global (<see cref="ServiceCategorySlug"/> null) row deactivates every other
/// active global row so <c>CommissionPolicyResolver</c>'s "most recent active" pick stays
/// unambiguous; likewise for a category-specific row sharing the same slug.</summary>
/// <remarks>A future <c>EffectiveFromUtc</c> schedules the new rate: the current one keeps applying
/// until then (the resolver picks the newest active policy already in effect), so there's never a gap.</remarks>
public record CreateCommissionPolicyCommand(string? ServiceCategorySlug, decimal Percentage, DateTime? EffectiveFromUtc = null) : IRequest<int>;

public class CreateCommissionPolicyValidator : AbstractValidator<CreateCommissionPolicyCommand>
{
    public CreateCommissionPolicyValidator(ILawPortalDbContext db)
    {
        RuleFor(x => x.Percentage).InclusiveBetween(0, 100);
        RuleFor(x => x.ServiceCategorySlug)
            .MustAsync(async (slug, ct) => slug is null || await db.ServiceCategories.AnyAsync(c => c.Slug == slug, ct))
            .WithMessage("Unknown service category.");
    }
}

public class CreateCommissionPolicyHandler(ILawPortalDbContext db) : IRequestHandler<CreateCommissionPolicyCommand, int>
{
    public async Task<int> Handle(CreateCommissionPolicyCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var effectiveFrom = request.EffectiveFromUtc is { } at && at > now ? at : now;
        if (effectiveFrom == now)
        {
            var superseded = await db.CommissionPolicies
                .Where(p => p.IsActive && p.ServiceCategorySlug == request.ServiceCategorySlug)
                .ToListAsync(cancellationToken);
            foreach (var old in superseded) old.IsActive = false;
        }

        var policy = new CommissionPolicy
        {
            ServiceCategorySlug = request.ServiceCategorySlug,
            Percentage = request.Percentage,
            IsActive = true,
            EffectiveFromUtc = effectiveFrom,
        };
        db.CommissionPolicies.Add(policy);

        await db.SaveChangesAsync(cancellationToken);
        return policy.Id;
    }
}
