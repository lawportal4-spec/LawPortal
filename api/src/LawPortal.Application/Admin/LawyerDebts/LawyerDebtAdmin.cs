using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Payments;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.LawyerDebts;

// «مبالغ مستحقة على المحامين»: who owes the platform money from refunds after payout, and collecting it.

public record LawyerDebtStatsDto(decimal TotalOutstanding, int LawyersOwing, decimal CollectedThisMonth, int LeftPlatformCount, decimal LeftPlatformAmount);

/// <param name="Collection">Offsetting (deducted from later payouts), LeftPlatform (account deleted: chased),
/// NoUpcomingPayouts (active but nothing to deduct from yet) or Settled.</param>
public record LawyerDebtSummaryDto(
    Guid LawyerProfileId, string FullName, bool AccountDeleted, DateTime? DeletedAtUtc, decimal Balance,
    DateTime? OldestOpenDebtAtUtc, int PaymentsCount, decimal HeldPayoutsTotal, DateTime? LastReminderAtUtc, string Collection);

public record LawyerDebtListDto(LawyerDebtStatsDto Stats, IReadOnlyList<LawyerDebtSummaryDto> Items);

public record LawyerDebtEntryDto(Guid Id, string Kind, decimal Amount, string? PaymentNumber, Guid? PaymentId, string? Reference, string? Note, DateTime CreatedAtUtc);

public record LawyerDebtDetailDto(
    LawyerDebtSummaryDto Summary, decimal TotalDebt, decimal TotalCollected, string? ContactEmail, string? ContactPhone,
    decimal SuspendedPayoutsTotal, int SuspendedPayoutsCount, IReadOnlyList<LawyerDebtEntryDto> Entries);

internal static class DebtRows
{
    public static async Task<List<LawyerDebtSummaryDto>> LoadAsync(ILawPortalDbContext db, Guid? onlyProfileId, CancellationToken cancellationToken)
    {
        var entries = await db.LawyerDebtEntries
            .Where(e => onlyProfileId == null || e.LawyerProfileId == onlyProfileId)
            .ToListAsync(cancellationToken);
        var ids = entries.Select(e => e.LawyerProfileId).Distinct().ToList();

        // Deleted accounts are hidden by the soft-delete filter; they're exactly the ones to chase.
        var people = await db.LawyerProfiles.IgnoreQueryFilters()
            .Where(l => ids.Contains(l.Id))
            .Select(l => new { l.Id, l.FullName, l.LastDebtReminderAtUtc, Deleted = l.User!.IsDeleted, l.User.DeletedAtUtc })
            .ToListAsync(cancellationToken);
        var held = await db.Payouts.Where(o => ids.Contains(o.LawyerProfileId) && o.Status == PayoutStatus.Held)
            .GroupBy(o => o.LawyerProfileId).Select(g => new { g.Key, Total = g.Sum(o => o.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);

        return people.Select(p =>
        {
            var mine = entries.Where(e => e.LawyerProfileId == p.Id).ToList();
            var balance = mine.Sum(e => e.Amount);
            var heldTotal = held.GetValueOrDefault(p.Id);
            var collection = balance <= 0 ? "Settled"
                : p.Deleted ? "LeftPlatform"
                : heldTotal > 0 ? "Offsetting" : "NoUpcomingPayouts";
            return new LawyerDebtSummaryDto(p.Id, p.FullName, p.Deleted, p.DeletedAtUtc, balance,
                mine.Where(e => e.Amount > 0).Min(e => (DateTime?)e.CreatedAtUtc),
                mine.Where(e => e.Amount > 0 && e.PaymentId != null).Select(e => e.PaymentId).Distinct().Count(),
                heldTotal, p.LastDebtReminderAtUtc, collection);
        }).ToList();
    }
}

public record GetLawyerDebtsQuery(string? Collection = null) : IRequest<LawyerDebtListDto>;

public class GetLawyerDebtsHandler(ILawPortalDbContext db) : IRequestHandler<GetLawyerDebtsQuery, LawyerDebtListDto>
{
    public async Task<LawyerDebtListDto> Handle(GetLawyerDebtsQuery request, CancellationToken cancellationToken)
    {
        var rows = await DebtRows.LoadAsync(db, null, cancellationToken);
        var owing = rows.Where(r => r.Balance > 0).ToList();
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var collected = await db.LawyerDebtEntries
            .Where(e => e.CreatedAtUtc >= monthStart && (e.Kind == LawyerDebtEntryKind.PayoutOffset || e.Kind == LawyerDebtEntryKind.BankTransfer))
            .SumAsync(e => (decimal?)-e.Amount, cancellationToken) ?? 0;
        var left = owing.Where(r => r.AccountDeleted).ToList();
        var stats = new LawyerDebtStatsDto(owing.Sum(r => r.Balance), owing.Count, collected, left.Count, left.Sum(r => r.Balance));

        var items = rows.Where(r => request.Collection switch
            {
                null or "" => r.Balance > 0,
                "Offsetting" => r.Collection is "Offsetting" or "NoUpcomingPayouts",
                _ => r.Collection == request.Collection,
            })
            .OrderByDescending(r => r.Balance).ToList();
        return new LawyerDebtListDto(stats, items);
    }
}

public record GetLawyerDebtQuery(Guid LawyerProfileId) : IRequest<LawyerDebtDetailDto>;

public class GetLawyerDebtHandler(ILawPortalDbContext db) : IRequestHandler<GetLawyerDebtQuery, LawyerDebtDetailDto>
{
    public async Task<LawyerDebtDetailDto> Handle(GetLawyerDebtQuery request, CancellationToken cancellationToken)
    {
        var summary = (await DebtRows.LoadAsync(db, request.LawyerProfileId, cancellationToken)).FirstOrDefault()
            ?? throw new KeyNotFoundException("This lawyer has no debt history.");
        var entries = await db.LawyerDebtEntries.Where(e => e.LawyerProfileId == request.LawyerProfileId)
            .OrderByDescending(e => e.CreatedAtUtc).ToListAsync(cancellationToken);
        var paymentIds = entries.Where(e => e.PaymentId != null).Select(e => e.PaymentId!.Value).Distinct().ToList();
        var numbers = await db.Payments.Where(p => paymentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Number, cancellationToken);
        var suspended = await db.Payouts.Where(o => o.LawyerProfileId == request.LawyerProfileId && o.Status == PayoutStatus.Suspended)
            .Select(o => o.Amount).ToListAsync(cancellationToken);
        var (email, phone) = await LawyerDebtContacts.FindAsync(db, request.LawyerProfileId, cancellationToken);

        return new LawyerDebtDetailDto(summary,
            entries.Where(e => e.Amount > 0).Sum(e => e.Amount), -entries.Where(e => e.Amount < 0).Sum(e => e.Amount),
            email, phone, suspended.Sum(), suspended.Count,
            entries.Select(e => new LawyerDebtEntryDto(e.Id, e.Kind.ToString(), e.Amount,
                e.PaymentId is { } pid ? numbers.GetValueOrDefault(pid) : null, e.PaymentId, e.Reference, e.Note, e.CreatedAtUtc)).ToList());
    }
}

/// <summary>Where to reach a lawyer about their debt: their account, or — if they deleted it — the
/// contact details kept for collection.</summary>
public static class LawyerDebtContacts
{
    public static async Task<(string? Email, string? Phone)> FindAsync(ILawPortalDbContext db, Guid lawyerProfileId, CancellationToken cancellationToken)
    {
        var user = await db.LawyerProfiles.IgnoreQueryFilters().Where(l => l.Id == lawyerProfileId)
            .Select(l => new { l.User!.Email, l.User.PhoneE164, l.User.IsDeleted }).FirstOrDefaultAsync(cancellationToken);
        if (user is { IsDeleted: false }) return (user.Email, user.PhoneE164);
        var kept = await db.DeletedAccountFingerprints.Where(f => f.LawyerProfileId == lawyerProfileId)
            .OrderByDescending(f => f.DeletedAtUtc).Select(f => new { f.ContactEmail, f.ContactPhone }).FirstOrDefaultAsync(cancellationToken);
        return (kept?.ContactEmail, kept?.ContactPhone);
    }
}

public record RecordLawyerDebtRepaymentCommand(Guid LawyerProfileId, decimal Amount, string Reference, string? Note) : IRequest<Unit>;

public class RecordLawyerDebtRepaymentValidator : AbstractValidator<RecordLawyerDebtRepaymentCommand>
{
    public RecordLawyerDebtRepaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordLawyerDebtRepaymentHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<RecordLawyerDebtRepaymentCommand, Unit>
{
    public async Task<Unit> Handle(RecordLawyerDebtRepaymentCommand request, CancellationToken cancellationToken)
    {
        var balance = await Payments.LawyerDebts.BalanceAsync(db, request.LawyerProfileId, cancellationToken);
        if (request.Amount > balance)
            throw new InvalidOperationException($"The lawyer owes {balance:0.00}; the payment can't be larger.");

        var entry = new LawyerDebtEntry
        {
            Id = Guid.NewGuid(),
            LawyerProfileId = request.LawyerProfileId,
            Kind = LawyerDebtEntryKind.BankTransfer,
            Amount = -request.Amount,
            Reference = request.Reference.Trim(),
            Note = request.Note?.Trim(),
            CreatedByUserId = currentUser.UserId,
        };
        db.LawyerDebtEntries.Add(entry);
        db.LedgerEntries.AddRange(LedgerPostingService.PostDebtRepayment(entry));
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("LawyerDebtRepaid", "LawyerProfile", request.LawyerProfileId.ToString(),
            $"{request.Amount:0.00} SAR · {request.Reference}", cancellationToken);
        return Unit.Value;
    }
}

public record SendLawyerDebtReminderCommand(Guid LawyerProfileId) : IRequest<Unit>;

public class SendLawyerDebtReminderHandler(ILawPortalDbContext db, IEmailSender emailSender, IAuditLogger auditLogger)
    : IRequestHandler<SendLawyerDebtReminderCommand, Unit>
{
    public async Task<Unit> Handle(SendLawyerDebtReminderCommand request, CancellationToken cancellationToken)
    {
        if (!await LawyerDebtReminders.SendAsync(db, emailSender, request.LawyerProfileId, cancellationToken))
            throw new InvalidOperationException("No email address is on file to remind this lawyer.");
        await auditLogger.LogAsync("LawyerDebtReminderSent", "LawyerProfile", request.LawyerProfileId.ToString(), cancellationToken: cancellationToken);
        return Unit.Value;
    }
}

/// <summary>The reminder email itself, shared by the button and the automatic sweep.</summary>
public static class LawyerDebtReminders
{
    public static async Task<bool> SendAsync(ILawPortalDbContext db, IEmailSender emailSender, Guid lawyerProfileId, CancellationToken cancellationToken)
    {
        var balance = await Payments.LawyerDebts.BalanceAsync(db, lawyerProfileId, cancellationToken);
        if (balance <= 0) return false;
        var (email, _) = await LawyerDebtContacts.FindAsync(db, lawyerProfileId, cancellationToken);
        if (string.IsNullOrWhiteSpace(email)) return false;

        var profile = await db.LawyerProfiles.IgnoreQueryFilters().FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);
        var html = $"""
            <div dir="rtl" style="font-family:Tahoma,Arial,sans-serif;font-size:15px;line-height:1.8">
              <p>أ. {System.Net.WebUtility.HtmlEncode(profile.FullName)}،</p>
              <p>عليك مبلغ مستحق لبوابة القانون قدره <b>{balance:0.00} ر.س</b>، بسبب استرجاع مبلغ لعميل بعد صرف مستحقاتك عن الطلب.</p>
              <p>يُخصم المبلغ تلقائيًا من مستحقاتك القادمة وفق شروط المحامين. إن لم تكن لديك مستحقات قادمة، يُرجى التواصل مع فريق بوابة القانون لتسويته.</p>
            </div>
            """;
        await emailSender.SendAsync(email, "مبلغ مستحق لبوابة القانون", html, cancellationToken);
        profile.LastDebtReminderAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record RefundPolicyDto(int RefundWindowDays, int DebtReminderIntervalDays);

public record GetRefundPolicyQuery : IRequest<RefundPolicyDto>;

public class GetRefundPolicyHandler(ILawPortalDbContext db) : IRequestHandler<GetRefundPolicyQuery, RefundPolicyDto>
{
    public async Task<RefundPolicyDto> Handle(GetRefundPolicyQuery request, CancellationToken cancellationToken)
    {
        var s = await RefundPolicy.GetAsync(db, cancellationToken);
        return new RefundPolicyDto(s.RefundWindowDays, s.DebtReminderIntervalDays);
    }
}

public record UpdateRefundPolicyCommand(int RefundWindowDays, int DebtReminderIntervalDays) : IRequest<RefundPolicyDto>;

public class UpdateRefundPolicyValidator : AbstractValidator<UpdateRefundPolicyCommand>
{
    public UpdateRefundPolicyValidator()
    {
        RuleFor(x => x.RefundWindowDays).InclusiveBetween(1, 365);
        RuleFor(x => x.DebtReminderIntervalDays).InclusiveBetween(1, 90);
    }
}

public class UpdateRefundPolicyHandler(ILawPortalDbContext db, IAuditLogger auditLogger) : IRequestHandler<UpdateRefundPolicyCommand, RefundPolicyDto>
{
    public async Task<RefundPolicyDto> Handle(UpdateRefundPolicyCommand request, CancellationToken cancellationToken)
    {
        var s = await db.RefundPolicySettings.FirstOrDefaultAsync(x => x.Id == RefundPolicySetting.SingletonId, cancellationToken);
        if (s is null)
        {
            s = new RefundPolicySetting { Id = RefundPolicySetting.SingletonId };
            db.RefundPolicySettings.Add(s);
        }
        s.RefundWindowDays = request.RefundWindowDays;
        s.DebtReminderIntervalDays = request.DebtReminderIntervalDays;
        s.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("RefundPolicyUpdated", nameof(RefundPolicySetting), s.Id.ToString(),
            $"window {s.RefundWindowDays}d, reminders every {s.DebtReminderIntervalDays}d", cancellationToken);
        return new RefundPolicyDto(s.RefundWindowDays, s.DebtReminderIntervalDays);
    }
}
