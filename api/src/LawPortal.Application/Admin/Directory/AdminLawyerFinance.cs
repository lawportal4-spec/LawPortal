using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Audit;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Directory;

// The lawyer's money with the platform: what they earned, what was paid, what's held, what they owe,
// and a statement explaining the balance line by line.

/// <param name="Balance">Held − debt. Positive: the platform owes the lawyer (the lawyer is دائن);
/// negative: the lawyer owes the platform (مدين).</param>
public record LawyerFinanceTotalsDto(decimal Earned, decimal PaidOut, decimal Held, decimal Suspended, decimal Debt, decimal Balance);
public record LawyerMonthDto(int Year, int Month, decimal PaidOut, decimal Held, decimal Deducted);
/// <param name="Amount">Positive raises what the platform owes the lawyer; negative lowers it.</param>
public record LawyerStatementLineDto(string Kind, decimal Amount, decimal RunningBalance, string? Reference, Guid? PaymentId, Guid? RequestId, string? Note, DateTime OccurredAtUtc);

public record AdminLawyerFinanceDto(
    LawyerFinanceTotalsDto Totals, IReadOnlyList<LawyerMonthDto> Months, IReadOnlyList<LawyerStatementLineDto> Statement,
    string AccountStatus, IReadOnlyList<ActivityDto> Activity);

public record GetAdminLawyerFinanceQuery(Guid LawyerProfileId) : IRequest<AdminLawyerFinanceDto>;

public class GetAdminLawyerFinanceHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminLawyerFinanceQuery, AdminLawyerFinanceDto>
{
    public async Task<AdminLawyerFinanceDto> Handle(GetAdminLawyerFinanceQuery request, CancellationToken cancellationToken)
    {
        var lawyer = await db.LawyerProfiles.IgnoreQueryFilters().Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == request.LawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Lawyer not found.");

        var payouts = await db.Payouts.Where(o => o.LawyerProfileId == lawyer.Id)
            .Select(o => new
            {
                o.Id, o.PaymentId, o.Amount, o.DebtOffset, o.Status, o.CreatedAtUtc, o.ReleasedAtUtc,
                Share = o.Payment!.NetToLawyerAmount, o.Payment.Number, RequestId = o.Payment.ServiceRequestId,
            })
            .ToListAsync(cancellationToken);
        var paymentIds = payouts.Select(p => p.PaymentId).ToList();
        // A refund before release takes the lawyer's share back out of escrow.
        var refundsBefore = await db.Refunds.Where(r => paymentIds.Contains(r.PaymentId) && r.Status == RefundStatus.Completed && r.LawyerShareBearer == null)
            .Select(r => new
            {
                r.PaymentId, r.CreatedAtUtc, r.Reason,
                Portion = db.LedgerEntries.Where(e => e.ReferenceType == "Refund" && e.ReferenceId == r.Id && e.Account == LedgerAccount.EscrowPayable && e.IsDebit)
                    .Sum(e => (decimal?)e.Amount) ?? 0,
            })
            .ToListAsync(cancellationToken);
        // Deductions are already inside each "paid out" line; a recovered overpayment is too (that line
        // shows the full amount released), so neither moves the statement's running balance twice.
        var debts = await db.LawyerDebtEntries.Where(e => e.LawyerProfileId == lawyer.Id
                && e.Kind != LawyerDebtEntryKind.PayoutOffset && e.Note != "OverpaidBeforeRefundFix")
            .ToListAsync(cancellationToken);
        var debtPaymentIds = debts.Where(d => d.PaymentId != null).Select(d => d.PaymentId!.Value).ToList();
        var debtPayments = await db.Payments.Where(p => debtPaymentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new { p.Number, p.ServiceRequestId }, cancellationToken);
        var byPayment = payouts.ToDictionary(p => p.PaymentId);

        var events = new List<(string Kind, decimal Amount, string? Ref, Guid? PaymentId, Guid? RequestId, string? Note, DateTime At)>();
        foreach (var o in payouts)
        {
            events.Add(("ShareCredited", o.Share, o.Number, o.PaymentId, o.RequestId, null, o.CreatedAtUtc));
            if (o.Status == PayoutStatus.Released)
                events.Add(("PaidOut", -(o.Amount - o.DebtOffset), o.Number, o.PaymentId, o.RequestId,
                    o.DebtOffset > 0 ? o.DebtOffset.ToString("0.00") : null, o.ReleasedAtUtc ?? o.CreatedAtUtc));
        }
        foreach (var r in refundsBefore.Where(r => r.Portion > 0))
            events.Add(("RefundBeforePayout", -r.Portion, byPayment[r.PaymentId].Number, r.PaymentId, byPayment[r.PaymentId].RequestId, r.Reason.ToString(), r.CreatedAtUtc));
        foreach (var d in debts)
        {
            var pay = d.PaymentId is { } pid ? debtPayments.GetValueOrDefault(pid) : null;
            events.Add((d.Kind.ToString(), -d.Amount, pay?.Number ?? d.Reference, d.PaymentId, pay?.ServiceRequestId, d.Note, d.CreatedAtUtc));
        }

        decimal running = 0;
        var statement = events.OrderBy(e => e.At).Select(e =>
        {
            running += e.Amount;
            return new LawyerStatementLineDto(e.Kind, e.Amount, running, e.Ref, e.PaymentId, e.RequestId, e.Note, e.At);
        }).Reverse().ToList();

        var held = payouts.Where(o => o.Status == PayoutStatus.Held).Sum(o => o.Amount);
        var suspended = payouts.Where(o => o.Status == PayoutStatus.Suspended).Sum(o => o.Amount);
        var paidOut = payouts.Where(o => o.Status == PayoutStatus.Released).Sum(o => o.Amount - o.DebtOffset);
        var debt = await db.LawyerDebtEntries.Where(e => e.LawyerProfileId == lawyer.Id).SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;
        var totals = new LawyerFinanceTotalsDto(paidOut + held + suspended, paidOut, held, suspended, debt, held + suspended - debt);

        var now = DateTime.UtcNow;
        var months = Enumerable.Range(0, 6).Select(i => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5 + i)).Select(m =>
        {
            bool In(DateTime? t) => t is { } v && v.Year == m.Year && v.Month == m.Month;
            return new LawyerMonthDto(m.Year, m.Month,
                payouts.Where(o => o.Status == PayoutStatus.Released && In(o.ReleasedAtUtc)).Sum(o => o.Amount - o.DebtOffset),
                payouts.Where(o => o.Status is PayoutStatus.Held or PayoutStatus.Suspended && In(o.CreatedAtUtc)).Sum(o => o.Amount),
                refundsBefore.Where(r => In(r.CreatedAtUtc)).Sum(r => r.Portion)
                    + debts.Where(d => d.Kind == LawyerDebtEntryKind.RefundAfterPayout && In(d.CreatedAtUtc)).Sum(d => d.Amount));
        }).ToList();

        var activity = await Activity.ForAsync(db, [lawyer.Id.ToString(), lawyer.UserId.ToString(), .. payouts.Select(p => p.Id.ToString())],
            lawyer.UserId, cancellationToken);
        return new AdminLawyerFinanceDto(totals, months, statement, Directory.AccountStatus.Of(lawyer.User!.Status, lawyer.User.IsDeleted), activity);
    }
}

/// <summary>Suspend or re-activate a client or lawyer account. A suspended account can't sign in or
/// refresh a session, and a suspended lawyer disappears from the directory.</summary>
public record SetAccountSuspendedCommand(Guid UserId, bool Suspended, string Reason) : IRequest<Unit>;

public class SetAccountSuspendedValidator : AbstractValidator<SetAccountSuspendedCommand>
{
    public SetAccountSuspendedValidator() => RuleFor(x => x.Reason).Must(r => r?.Trim().Length >= 5).WithMessage("Give a reason (at least 5 characters).").MaximumLength(500);
}

public class SetAccountSuspendedHandler(ILawPortalDbContext db, IAuditLogger auditLogger) : IRequestHandler<SetAccountSuspendedCommand, Unit>
{
    public async Task<Unit> Handle(SetAccountSuspendedCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.LawyerProfile).ThenInclude(l => l!.License)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Account not found.");
        if (user.UserType == UserType.Admin) throw new InvalidOperationException("Admin accounts are managed from Users and Roles.");

        if (request.Suspended)
        {
            if (user.Status is UserStatus.Suspended or UserStatus.Banned) throw new InvalidOperationException("This account is already suspended.");
            user.Status = UserStatus.Suspended;
            if (user.LawyerProfile is { } lawyer) lawyer.IsVerified = false;
            var tokens = await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(cancellationToken);
            foreach (var token in tokens) token.RevokedAtUtc = DateTime.UtcNow;
        }
        else
        {
            if (user.Status is not (UserStatus.Suspended or UserStatus.Banned)) throw new InvalidOperationException("This account isn't suspended.");
            user.Status = UserStatus.Active;
            if (user.LawyerProfile is { License.VerificationStatus: LicenseVerificationStatus.Approved } lawyer) lawyer.IsVerified = true;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(request.Suspended ? "AccountSuspended" : "AccountReactivated", nameof(User), user.Id.ToString(),
            request.Reason.Trim(), cancellationToken);
        return Unit.Value;
    }
}

public record AdminNoteDto(Guid Id, string Body, string? AuthorName, DateTime CreatedAtUtc);

public record GetAdminNotesQuery(string EntityType, Guid EntityId) : IRequest<IReadOnlyList<AdminNoteDto>>;

public class GetAdminNotesHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminNotesQuery, IReadOnlyList<AdminNoteDto>>
{
    public async Task<IReadOnlyList<AdminNoteDto>> Handle(GetAdminNotesQuery request, CancellationToken cancellationToken) =>
        await db.AdminNotes.Where(n => n.EntityType == request.EntityType && n.EntityId == request.EntityId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new AdminNoteDto(n.Id, n.Body, n.AuthorName, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);
}

public record AddAdminNoteCommand(string EntityType, Guid EntityId, string Body) : IRequest<Unit>;

public class AddAdminNoteValidator : AbstractValidator<AddAdminNoteCommand>
{
    public AddAdminNoteValidator()
    {
        RuleFor(x => x.EntityType).Must(t => t is "Client" or "Lawyer" or "Request");
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public class AddAdminNoteHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<AddAdminNoteCommand, Unit>
{
    public async Task<Unit> Handle(AddAdminNoteCommand request, CancellationToken cancellationToken)
    {
        var author = await db.Users.Where(u => u.Id == currentUser.UserId)
            .Select(u => u.AdminProfile != null ? u.AdminProfile.DisplayName : u.Email).FirstOrDefaultAsync(cancellationToken);
        db.AdminNotes.Add(new AdminNote
        {
            Id = Guid.NewGuid(), EntityType = request.EntityType, EntityId = request.EntityId,
            Body = request.Body.Trim(), AuthorUserId = currentUser.UserId, AuthorName = author,
        });
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

/// <param name="Kind">Client, Lawyer, Request or Payment — where the result opens.</param>
public record AdminSearchHitDto(string Kind, Guid Id, string Title, string? Subtitle);

/// <summary>The search box at the top of the admin: phone, name, request or payment number, licence number.</summary>
public record AdminSearchQuery(string Q) : IRequest<IReadOnlyList<AdminSearchHitDto>>;

public class AdminSearchHandler(ILawPortalDbContext db) : IRequestHandler<AdminSearchQuery, IReadOnlyList<AdminSearchHitDto>>
{
    public async Task<IReadOnlyList<AdminSearchHitDto>> Handle(AdminSearchQuery request, CancellationToken cancellationToken)
    {
        var q = request.Q.Trim();
        if (q.Length < 2) return [];
        var hits = new List<AdminSearchHitDto>();
        hits.AddRange(await db.ClientProfiles.IgnoreQueryFilters()
            .Where(c => (c.FullName != null && c.FullName.Contains(q)) || (c.User!.PhoneE164 != null && c.User.PhoneE164.Contains(Activity.PhoneDigits(q))))
            .Take(5).Select(c => new AdminSearchHitDto("Client", c.Id, c.FullName ?? c.User!.PhoneE164 ?? "—", c.User!.PhoneE164))
            .ToListAsync(cancellationToken));
        hits.AddRange(await db.LawyerProfiles.IgnoreQueryFilters()
            .Where(l => l.FullName.Contains(q) || (l.User!.PhoneE164 != null && l.User.PhoneE164.Contains(Activity.PhoneDigits(q)))
                || (l.User.Email != null && l.User.Email.Contains(q)) || (l.License != null && l.License.LicenseNumber.Contains(q)))
            .Take(5).Select(l => new AdminSearchHitDto("Lawyer", l.Id, l.FullName, l.License != null ? l.License.LicenseNumber : null))
            .ToListAsync(cancellationToken));
        hits.AddRange(await db.ServiceRequests.Where(r => r.Number.Contains(q))
            .Take(5).Select(r => new AdminSearchHitDto("Request", r.Id, r.Number, r.Title)).ToListAsync(cancellationToken));
        hits.AddRange(await db.Payments.Where(p => p.Number.Contains(q))
            .Take(5).Select(p => new AdminSearchHitDto("Payment", p.Id, p.Number, null)).ToListAsync(cancellationToken));
        return hits;
    }
}
