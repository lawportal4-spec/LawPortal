using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

public record GetAdminPaymentDetailQuery(Guid PaymentId) : IRequest<AdminPaymentDetailDto>;

public class GetAdminPaymentDetailHandler(ILawPortalDbContext db, IPaymentGateway gateway) : IRequestHandler<GetAdminPaymentDetailQuery, AdminPaymentDetailDto>
{
    public async Task<AdminPaymentDetailDto> Handle(GetAdminPaymentDetailQuery request, CancellationToken cancellationToken)
    {
        var p = await db.Payments
            .Include(x => x.Client).ThenInclude(c => c!.User)
            .Include(x => x.LawyerProfile)
            .Include(x => x.ServiceRequest)
            .Include(x => x.Refunds)
            .Include(x => x.Payout)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");

        // Older payments (and any whose lookup failed at confirmation) get their bank details once, here.
        if (PaymentTransactionDetails.NeedsFetch(p, gateway)
            && await PaymentTransactionDetails.TryFetchAsync(gateway, p, cancellationToken) is { } fetched)
        {
            p.Transaction = fetched;
            await db.SaveChangesAsync(cancellationToken);
        }

        var walletTransactionId = p.MethodDescription == "Wallet"
            ? await db.WalletTransactions.Where(w => w.PaymentId == p.Id).OrderBy(w => w.CreatedAtUtc).Select(w => (Guid?)w.Id).FirstOrDefaultAsync(cancellationToken)
            : null;
        var t = p.Transaction;

        // Each refund's effect on every share, read from its own ledger posting.
        var refundIds = p.Refunds.Select(r => r.Id).ToList();
        var refundLines = await db.LedgerEntries
            .Where(e => e.ReferenceType == "Refund" && refundIds.Contains(e.ReferenceId))
            .ToListAsync(cancellationToken);
        decimal Portion(Guid refundId, LedgerAccount account) =>
            refundLines.Where(e => e.ReferenceId == refundId && e.Account == account).Sum(e => e.Amount);

        var discount = p.DiscountCodeId is { } codeId
            ? await db.DiscountCodes.Where(d => d.Id == codeId).Select(d => new { d.Id, d.Code, d.Kind, d.Value, d.MaxDiscountAmount, d.Scopes }).FirstOrDefaultAsync(cancellationToken)
            : null;
        RefundWindowDto? refundWindow = null;
        if (p.Payout is { Status: PayoutStatus.Released } released)
        {
            var policy = await RefundPolicy.GetAsync(db, cancellationToken);
            var deadline = RefundPolicy.DeadlineFor(released, policy);
            refundWindow = new RefundWindowDto(policy.RefundWindowDays, deadline, DateTime.UtcNow > deadline);
        }

        var requestType = p.ServiceRequest switch
        {
            ConsultationRequest c => c.ConsultationType.ToString(),
            BiddingRequest => "Bidding",
            CatalogRequest => "Catalog",
            _ => null,
        };

        return new AdminPaymentDetailDto(
            p.Id, p.Number, p.Purpose.ToString(), p.Status.ToString(), p.MethodDescription,
            p.Total, p.VatAmount, p.CommissionAmount, p.NetToLawyerAmount, p.IsVatApplicable,
            p.Client?.FullName ?? p.Client?.User?.PhoneE164, p.LawyerProfile?.FullName, p.ServiceRequest?.Number,
            p.GatewayProvider, p.GatewayPaymentId, p.FailureReason, p.CreatedAtUtc, p.PaidAtUtc,
            p.Refunds.OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new RefundLineDto(r.Id, r.Amount, r.Reason.ToString(), r.Details, r.Status.ToString(), r.CreatedAtUtc, r.GatewayRefundId,
                    // The lawyer's share comes back from escrow, or — after payout — as their debt or the platform's loss.
                    Portion(r.Id, LedgerAccount.EscrowPayable) + Portion(r.Id, LedgerAccount.LawyerReceivable) + Portion(r.Id, LedgerAccount.RefundLossExpense),
                    Portion(r.Id, LedgerAccount.VatPayable),
                    Portion(r.Id, LedgerAccount.CommissionRevenue), Portion(r.Id, LedgerAccount.DiscountExpense),
                    r.LawyerShareBearer?.ToString())).ToList(),
            p.Payout is null ? null : new PayoutLineDto(p.Payout.Id, p.Payout.Amount, p.Payout.Status.ToString(), p.Payout.CreatedAtUtc, p.Payout.ReleasedAtUtc,
                p.Payout.DebtOffset, p.Payout.LawyerProfileId, await Payments.LawyerDebts.BalanceAsync(db, p.Payout.LawyerProfileId, cancellationToken)),
            p.GrossAmount, p.DiscountAmount,
            t is null ? null : new GatewayTransactionDto(t.TransactionId, t.SourceType, t.CardBrand, t.CardMasked, t.ReferenceNumber,
                t.AuthorizationCode, t.ResponseCode, t.Message, t.Fee, t.FetchedAtUtc),
            walletTransactionId,
            discount is null ? null : new PaymentDiscountDto(discount.Id, discount.Code, discount.Kind.ToString(), discount.Value, discount.MaxDiscountAmount,
                Enum.GetValues<DiscountScope>().Where(s => s != DiscountScope.None && discount.Scopes.HasFlag(s)).Select(s => s.ToString()).ToList()),
            LedgerPostingService.DiscountExpenseOf(p),
            requestType,
            refundWindow,
            p.LawyerProfileId,
            p.Client?.User?.PhoneE164,
            p.ClientId,
            p.ServiceRequestId);
    }
}
