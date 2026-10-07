using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Payments;
using LawPortal.Domain.Billing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Admin.DiscountCodes;

/// <param name="Scopes"><see cref="DiscountScope"/> names, e.g. <c>["InstantConsultation"]</c>.</param>
/// <param name="Used">Confirmed (paid) redemptions.</param>
public record AdminDiscountCodeDto(
    Guid Id, string Code, string? DescriptionAr, string? DescriptionEn, string Kind, decimal Value,
    decimal? MaxDiscountAmount, decimal? MinAmount, IReadOnlyList<string> Scopes, int? UsageLimit, int? PerUserLimit,
    bool FirstPaymentOnly, bool IsActive, DateTime? StartsAtUtc, DateTime? EndsAtUtc, int Used, decimal TotalDiscounted, DateTime CreatedAtUtc,
    /// <summary>Where a shared promotion should send people: the lawyer registration page for
    /// lawyer-only codes, otherwise the client site.</summary>
    string ShareUrl);

public record DiscountRedemptionDto(Guid Id, string UserName, string Scope, Guid ReferenceId, decimal Amount, string Status, DateTime CreatedAtUtc);

/// <summary>The status badge on the admin list, in the same precedence: off, expired, used up, scheduled, active.</summary>
public enum DiscountCodeListStatus { Active, Scheduled, Expired, UsedUp, Inactive }

/// <summary>Admin list with search (code or description) and filters. <paramref name="ValidFrom"/> /
/// <paramref name="ValidTo"/> keep codes whose validity period overlaps that range.</summary>
public record GetDiscountCodesQuery(
    string? Search = null,
    DiscountCodeListStatus? Status = null,
    DiscountScope? Scope = null,
    DiscountKind? Kind = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<AdminDiscountCodeDto>>;

public class GetDiscountCodesHandler(ILawPortalDbContext db, IConfiguration configuration) : IRequestHandler<GetDiscountCodesQuery, PagedResult<AdminDiscountCodeDto>>
{
    public async Task<PagedResult<AdminDiscountCodeDto>> Handle(GetDiscountCodesQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.DiscountCodes.Select(c => new
        {
            Code = c,
            Used = c.Redemptions.Count(r => r.Status == DiscountRedemptionStatus.Confirmed),
            Total = c.Redemptions.Where(r => r.Status == DiscountRedemptionStatus.Confirmed).Sum(r => (decimal?)r.Amount) ?? 0,
        });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Code.Code.Contains(term.ToUpper())
                || (x.Code.DescriptionAr != null && x.Code.DescriptionAr.Contains(term))
                || (x.Code.DescriptionEn != null && x.Code.DescriptionEn.Contains(term)));
        }
        if (request.Scope is { } scope) query = query.Where(x => (x.Code.Scopes & scope) != 0);
        if (request.Kind is { } kind) query = query.Where(x => x.Code.Kind == kind);
        if (request.ValidFrom is { } from) query = query.Where(x => x.Code.EndsAtUtc == null || x.Code.EndsAtUtc >= from);
        if (request.ValidTo is { } to) query = query.Where(x => x.Code.StartsAtUtc == null || x.Code.StartsAtUtc <= to);

        query = request.Status switch
        {
            DiscountCodeListStatus.Inactive => query.Where(x => !x.Code.IsActive),
            DiscountCodeListStatus.Expired => query.Where(x => x.Code.IsActive && x.Code.EndsAtUtc <= now),
            DiscountCodeListStatus.UsedUp => query.Where(x => x.Code.IsActive && !(x.Code.EndsAtUtc <= now)
                && x.Code.UsageLimit != null && x.Used >= x.Code.UsageLimit),
            DiscountCodeListStatus.Scheduled => query.Where(x => x.Code.IsActive && !(x.Code.EndsAtUtc <= now)
                && !(x.Code.UsageLimit != null && x.Used >= x.Code.UsageLimit) && x.Code.StartsAtUtc > now),
            DiscountCodeListStatus.Active => query.Where(x => x.Code.IsActive && !(x.Code.EndsAtUtc <= now)
                && !(x.Code.UsageLimit != null && x.Used >= x.Code.UsageLimit) && !(x.Code.StartsAtUtc > now)),
            _ => query,
        };

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.Code.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var items = rows.Select(r => ToDto(r.Code, r.Used, r.Total, configuration)).ToList();
        return new PagedResult<AdminDiscountCodeDto>(items, page, pageSize, total);
    }

    internal static AdminDiscountCodeDto ToDto(DiscountCode c, int used, decimal totalDiscounted, IConfiguration configuration) => new(
        c.Id, c.Code, c.DescriptionAr, c.DescriptionEn, c.Kind.ToString(), c.Value,
        c.MaxDiscountAmount, c.MinAmount, ScopeNames(c.Scopes), c.UsageLimit, c.PerUserLimit,
        c.FirstPaymentOnly, c.IsActive, c.StartsAtUtc, c.EndsAtUtc, used, totalDiscounted, c.CreatedAtUtc,
        ShareUrlOf(c.Scopes, configuration));

    private const DiscountScope LawyerScopes = DiscountScope.LawyerSubscription | DiscountScope.LawyerRegistrationFee;

    private static string ShareUrlOf(DiscountScope scopes, IConfiguration configuration) =>
        (scopes & ~LawyerScopes) == 0
            ? (configuration["Payments:LawyerBaseUrl"] ?? "http://localhost:5174").TrimEnd('/') + "/register"
            : (configuration["Payments:ClientBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

    private static IReadOnlyList<string> ScopeNames(DiscountScope scopes) =>
        Enum.GetValues<DiscountScope>().Where(s => s != DiscountScope.None && scopes.HasFlag(s)).Select(s => s.ToString()).ToList();
}

public record GetDiscountCodeQuery(Guid Id) : IRequest<AdminDiscountCodeDto>;

public class GetDiscountCodeHandler(ILawPortalDbContext db, IConfiguration configuration) : IRequestHandler<GetDiscountCodeQuery, AdminDiscountCodeDto>
{
    public async Task<AdminDiscountCodeDto> Handle(GetDiscountCodeQuery request, CancellationToken cancellationToken)
    {
        var row = await db.DiscountCodes.Where(c => c.Id == request.Id)
            .Select(c => new
            {
                Code = c,
                Used = c.Redemptions.Count(r => r.Status == DiscountRedemptionStatus.Confirmed),
                Total = c.Redemptions.Where(r => r.Status == DiscountRedemptionStatus.Confirmed).Sum(r => (decimal?)r.Amount) ?? 0,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Discount code not found.");
        return GetDiscountCodesHandler.ToDto(row.Code, row.Used, row.Total, configuration);
    }
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
        if (request.Id is null && await db.DiscountCodes.AnyAsync(c => c.Code == normalized, cancellationToken))
            throw new InvalidOperationException("A discount code with this code already exists.");

        DiscountCode code;
        if (request.Id is { } id)
        {
            // The code text is fixed once created: renaming a code people have used would
            // muddle its redemption history. Everything else stays editable.
            code = await db.DiscountCodes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException("Discount code not found.");
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
