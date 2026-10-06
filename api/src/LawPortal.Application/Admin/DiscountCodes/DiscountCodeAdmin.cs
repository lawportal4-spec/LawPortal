using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments;
using LawPortal.Domain.Billing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.DiscountCodes;

/// <param name="Scopes"><see cref="DiscountScope"/> names, e.g. <c>["InstantConsultation"]</c>.</param>
/// <param name="Used">Confirmed (paid) redemptions.</param>
public record AdminDiscountCodeDto(
    Guid Id, string Code, string? DescriptionAr, string? DescriptionEn, string Kind, decimal Value,
    decimal? MaxDiscountAmount, decimal? MinAmount, IReadOnlyList<string> Scopes, int? UsageLimit, int? PerUserLimit,
    bool FirstPaymentOnly, bool IsActive, DateTime? StartsAtUtc, DateTime? EndsAtUtc, int Used, decimal TotalDiscounted, DateTime CreatedAtUtc);

public record DiscountRedemptionDto(Guid Id, string UserName, string Scope, Guid ReferenceId, decimal Amount, string Status, DateTime CreatedAtUtc);

public record GetDiscountCodesQuery : IRequest<IReadOnlyList<AdminDiscountCodeDto>>;

public class GetDiscountCodesHandler(ILawPortalDbContext db) : IRequestHandler<GetDiscountCodesQuery, IReadOnlyList<AdminDiscountCodeDto>>
{
    public async Task<IReadOnlyList<AdminDiscountCodeDto>> Handle(GetDiscountCodesQuery request, CancellationToken cancellationToken)
    {
        var rows = await db.DiscountCodes
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new
            {
                Code = c,
                Used = c.Redemptions.Count(r => r.Status == DiscountRedemptionStatus.Confirmed),
                Total = c.Redemptions.Where(r => r.Status == DiscountRedemptionStatus.Confirmed).Sum(r => (decimal?)r.Amount) ?? 0,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new AdminDiscountCodeDto(
            r.Code.Id, r.Code.Code, r.Code.DescriptionAr, r.Code.DescriptionEn, r.Code.Kind.ToString(), r.Code.Value,
            r.Code.MaxDiscountAmount, r.Code.MinAmount, ScopeNames(r.Code.Scopes), r.Code.UsageLimit, r.Code.PerUserLimit,
            r.Code.FirstPaymentOnly, r.Code.IsActive, r.Code.StartsAtUtc, r.Code.EndsAtUtc, r.Used, r.Total, r.Code.CreatedAtUtc)).ToList();
    }

    private static IReadOnlyList<string> ScopeNames(DiscountScope scopes) =>
        Enum.GetValues<DiscountScope>().Where(s => s != DiscountScope.None && scopes.HasFlag(s)).Select(s => s.ToString()).ToList();
}

public record GetDiscountRedemptionsQuery(Guid DiscountCodeId) : IRequest<IReadOnlyList<DiscountRedemptionDto>>;

public class GetDiscountRedemptionsHandler(ILawPortalDbContext db) : IRequestHandler<GetDiscountRedemptionsQuery, IReadOnlyList<DiscountRedemptionDto>>
{
    public async Task<IReadOnlyList<DiscountRedemptionDto>> Handle(GetDiscountRedemptionsQuery request, CancellationToken cancellationToken) =>
        await db.DiscountRedemptions
            .Where(r => r.DiscountCodeId == request.DiscountCodeId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(100)
            .Select(r => new DiscountRedemptionDto(
                r.Id,
                db.ClientProfiles.Where(c => c.UserId == r.UserId).Select(c => c.FullName).FirstOrDefault()
                    ?? db.LawyerProfiles.Where(l => l.UserId == r.UserId).Select(l => l.FullName).FirstOrDefault()
                    ?? db.Users.Where(u => u.Id == r.UserId).Select(u => u.PhoneE164).FirstOrDefault() ?? "",
                r.Scope.ToString(), r.ReferenceId, r.Amount, r.Status.ToString(), r.CreatedAtUtc))
            .ToListAsync(cancellationToken);
}

/// <summary>Creates a code when <paramref name="Id"/> is null, otherwise replaces every field of it
/// (that's also how the admin page turns a code on and off).</summary>
public record SaveDiscountCodeCommand(
    Guid? Id, string Code, string? DescriptionAr, string? DescriptionEn, DiscountKind Kind, decimal Value,
    decimal? MaxDiscountAmount, decimal? MinAmount, IReadOnlyList<DiscountScope> Scopes, int? UsageLimit, int? PerUserLimit,
    bool FirstPaymentOnly, bool IsActive, DateTime? StartsAtUtc, DateTime? EndsAtUtc) : IRequest<Guid>;

public class SaveDiscountCodeValidator : AbstractValidator<SaveDiscountCodeCommand>
{
    public SaveDiscountCodeValidator()
    {
        RuleFor(x => DiscountService.Normalize(x.Code)).Matches("^[A-Z0-9_-]{3,40}$").WithName("Code");
        RuleFor(x => x.DescriptionAr).MaximumLength(300);
        RuleFor(x => x.DescriptionEn).MaximumLength(300);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.Kind == DiscountKind.Percentage);
        RuleFor(x => x.MaxDiscountAmount).GreaterThan(0);
        RuleFor(x => x.MinAmount).GreaterThan(0);
        RuleFor(x => x.Scopes).NotEmpty();
        RuleForEach(x => x.Scopes).IsInEnum().NotEqual(DiscountScope.None);
        RuleFor(x => x.UsageLimit).GreaterThan(0);
        RuleFor(x => x.PerUserLimit).GreaterThan(0);
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc).When(x => x.StartsAtUtc.HasValue && x.EndsAtUtc.HasValue);
    }
}

public class SaveDiscountCodeHandler(ILawPortalDbContext db, IAuditLogger auditLogger) : IRequestHandler<SaveDiscountCodeCommand, Guid>
{
    public async Task<Guid> Handle(SaveDiscountCodeCommand request, CancellationToken cancellationToken)
    {
        var normalized = DiscountService.Normalize(request.Code);
        if (await db.DiscountCodes.AnyAsync(c => c.Code == normalized && c.Id != request.Id, cancellationToken))
            throw new InvalidOperationException("A discount code with this code already exists.");

        DiscountCode code;
        if (request.Id is { } id)
        {
            code = await db.DiscountCodes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException("Discount code not found.");
            code.Code = normalized;
        }
        else
        {
            code = new DiscountCode { Id = Guid.NewGuid(), Code = normalized };
            db.DiscountCodes.Add(code);
        }

        code.DescriptionAr = request.DescriptionAr;
        code.DescriptionEn = request.DescriptionEn;
        code.Kind = request.Kind;
        code.Value = request.Value;
        code.MaxDiscountAmount = request.Kind == DiscountKind.Percentage ? request.MaxDiscountAmount : null;
        code.MinAmount = request.MinAmount;
        code.Scopes = request.Scopes.Aggregate(DiscountScope.None, (all, s) => all | s);
        code.UsageLimit = request.UsageLimit;
        code.PerUserLimit = request.PerUserLimit;
        code.FirstPaymentOnly = request.FirstPaymentOnly;
        code.IsActive = request.IsActive;
        code.StartsAtUtc = request.StartsAtUtc;
        code.EndsAtUtc = request.EndsAtUtc;

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(request.Id is null ? "DiscountCodeCreated" : "DiscountCodeUpdated", nameof(DiscountCode), code.Id.ToString(), cancellationToken: cancellationToken);
        return code.Id;
    }
}
