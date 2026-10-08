using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

/// <summary>The ledger as journal entries: lines grouped by the record that caused them (every
/// posting in <c>LedgerPostingService</c> shares one ReferenceType + ReferenceId), newest first.
/// <c>Search</c> matches the PAY-/SUB-/REG- number or a line's description; <c>Kind</c> as in
/// <see cref="JournalEntryDto"/>.</summary>
public record GetLedgerJournalQuery(
    string? Search = null,
    string? Kind = null,
    LedgerAccount? Account = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 15) : IRequest<PagedResult<JournalEntryDto>>;

public class GetLedgerJournalHandler(ILawPortalDbContext db) : IRequestHandler<GetLedgerJournalQuery, PagedResult<JournalEntryDto>>
{
    public async Task<PagedResult<JournalEntryDto>> Handle(GetLedgerJournalQuery request, CancellationToken cancellationToken)
    {
        var entries = db.LedgerEntries.AsQueryable();
        if (request.From is { } from) entries = entries.Where(e => e.CreatedAtUtc >= from);
        if (request.To is { } to) entries = entries.Where(e => e.CreatedAtUtc <= to);

        var checkouts = db.Payments.Where(p => p.Purpose == PaymentPurpose.RequestCheckout).Select(p => p.Id);
        var topUps = db.Payments.Where(p => p.Purpose == PaymentPurpose.WalletTopUp).Select(p => p.Id);
        entries = request.Kind switch
        {
            "Checkout" => entries.Where(e => e.ReferenceType == "Payment" && checkouts.Contains(e.ReferenceId)),
            "TopUp" => entries.Where(e => e.ReferenceType == "Payment" && topUps.Contains(e.ReferenceId)),
            "Refund" => entries.Where(e => e.ReferenceType == "Refund"),
            "Payout" => entries.Where(e => e.ReferenceType == "Payout"),
            "DebtRepayment" => entries.Where(e => e.ReferenceType == "LawyerDebtRepayment"),
            "Subscription" => entries.Where(e => e.ReferenceType == "SubscriptionInvoice"),
            "RegistrationFee" => entries.Where(e => e.ReferenceType == "RegistrationFeeInvoice"),
            _ => entries,
        };

        // Filters below pick whole postings, so the matching postings keep all their lines.
        var postings = entries;
        if (request.Account is { } account)
        {
            var withAccount = entries.Where(e => e.Account == account).Select(e => e.ReferenceId);
            postings = postings.Where(e => withAccount.Contains(e.ReferenceId));
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var payments = db.Payments.Where(p => p.Number.Contains(term)).Select(p => p.Id);
            var refunds = db.Refunds.Where(r => payments.Contains(r.PaymentId)).Select(r => r.Id);
            var payouts = db.Payouts.Where(o => payments.Contains(o.PaymentId)).Select(o => o.Id);
            var subs = db.SubscriptionInvoices.Where(i => i.Number.Contains(term)).Select(i => i.Id);
            var regs = db.RegistrationFeeInvoices.Where(i => i.Number.Contains(term)).Select(i => i.Id);
            var described = entries.Where(e => e.Description != null && e.Description.Contains(term)).Select(e => e.ReferenceId);
            postings = postings.Where(e => payments.Contains(e.ReferenceId) || refunds.Contains(e.ReferenceId) || payouts.Contains(e.ReferenceId)
                || subs.Contains(e.ReferenceId) || regs.Contains(e.ReferenceId) || described.Contains(e.ReferenceId));
        }

        var keys = postings
            .GroupBy(e => new { e.ReferenceType, e.ReferenceId })
            .Select(g => new { g.Key.ReferenceType, g.Key.ReferenceId, At = g.Min(e => e.CreatedAtUtc) });

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var total = await keys.CountAsync(cancellationToken);
        var pageKeys = await keys.OrderByDescending(k => k.At).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var ids = pageKeys.Select(k => k.ReferenceId).ToList();
        var lines = await db.LedgerEntries.Where(e => ids.Contains(e.ReferenceId)).ToListAsync(cancellationToken);

        // Resolve the readable number (and the payment to link to) for each posting.
        var paymentsById = await db.Payments.Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Number, p.Purpose }).ToDictionaryAsync(p => p.Id, cancellationToken);
        var refundPayments = await db.Refunds.Where(r => ids.Contains(r.Id)).Select(r => new { r.Id, r.PaymentId, r.Payment!.Number }).ToDictionaryAsync(r => r.Id, cancellationToken);
        var payoutPayments = await db.Payouts.Where(o => ids.Contains(o.Id)).Select(o => new { o.Id, o.PaymentId, o.Payment!.Number }).ToDictionaryAsync(o => o.Id, cancellationToken);
        var subNumbers = await db.SubscriptionInvoices.Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.Number, cancellationToken);
        var repaymentRefs = await db.LawyerDebtEntries.Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Reference, cancellationToken);
        var regNumbers = await db.RegistrationFeeInvoices.Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.Number, cancellationToken);

        var items = pageKeys.Select(k =>
        {
            (string kind, string? number, Guid? paymentId) = k.ReferenceType switch
            {
                "Payment" when paymentsById.TryGetValue(k.ReferenceId, out var p) =>
                    (p.Purpose == PaymentPurpose.WalletTopUp ? "TopUp" : "Checkout", p.Number, (Guid?)p.Id),
                "Refund" when refundPayments.TryGetValue(k.ReferenceId, out var r) => ("Refund", r.Number, r.PaymentId),
                "Payout" when payoutPayments.TryGetValue(k.ReferenceId, out var o) => ("Payout", o.Number, o.PaymentId),
                "SubscriptionInvoice" => ("Subscription", subNumbers.GetValueOrDefault(k.ReferenceId), null),
                "RegistrationFeeInvoice" => ("RegistrationFee", regNumbers.GetValueOrDefault(k.ReferenceId), null),
                "LawyerDebtRepayment" => ("DebtRepayment", repaymentRefs.GetValueOrDefault(k.ReferenceId), null),
                "LawyerDebtCorrection" => ("DebtCorrection", null, null),
                _ => (k.ReferenceType, null, null),
            };
            var posting = lines.Where(l => l.ReferenceId == k.ReferenceId && l.ReferenceType == k.ReferenceType)
                .OrderByDescending(l => l.IsDebit)
                .Select(l => new JournalLineDto(l.Account.ToString(), l.IsDebit, l.Amount, l.Description))
                .ToList();
            return new JournalEntryDto(k.ReferenceType, k.ReferenceId, kind, number, paymentId, k.At, posting);
        }).ToList();

        return new PagedResult<JournalEntryDto>(items, page, pageSize, total);
    }
}
