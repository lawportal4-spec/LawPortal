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
public record CreateCommissionPolicyCommand(string? ServiceCategorySlug, decimal Percentage) : IRequest<int>;

public class CreateCommissionPolicyValidator : AbstractValidator<CreateCommissionPolicyCommand>
{
    public CreateCommissionPolicyValidator() => RuleFor(x => x.Percentage).InclusiveBetween(0, 100);
}

public class CreateCommissionPolicyHandler(ILawPortalDbContext db) : IRequestHandler<CreateCommissionPolicyCommand, int>
{
    public async Task<int> Handle(CreateCommissionPolicyCommand request, CancellationToken cancellationToken)
    {
        var superseded = await db.CommissionPolicies
            .Where(p => p.IsActive && p.ServiceCategorySlug == request.ServiceCategorySlug)
            .ToListAsync(cancellationToken);
        foreach (var old in superseded) old.IsActive = false;

        var policy = new CommissionPolicy
        {
            ServiceCategorySlug = request.ServiceCategorySlug,
            Percentage = request.Percentage,
            IsActive = true,
            EffectiveFromUtc = DateTime.UtcNow,
        };
        db.CommissionPolicies.Add(policy);

        await db.SaveChangesAsync(cancellationToken);
        return policy.Id;
    }
}
