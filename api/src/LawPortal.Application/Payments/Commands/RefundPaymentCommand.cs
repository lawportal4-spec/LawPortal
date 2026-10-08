using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Wallet;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using LawPortal.Domain.Wallet;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments.Commands;

/// <param name="LawyerShareBearer">Only for a payment whose payout was already released: who covers the
/// lawyer's share. Defaults to the lawyer, who then owes it (deducted from later payouts).</param>
public record RefundPaymentCommand(Guid PaymentId, decimal Amount, RefundReason Reason, string? Details = null,
    RefundBearer? LawyerShareBearer = null) : IRequest<Unit>;

public class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.LawyerShareBearer).IsInEnum().When(x => x.LawyerShareBearer is not null);
        RuleFor(x => x.Details).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty().When(x => x.Reason == RefundReason.Other).WithMessage("Describe the reason when choosing Other.");
    }
}

public class RefundPaymentHandler(ILawPortalDbContext db, IPaymentGateway gateway, ICurrentUser currentUser, IAuditLogger auditLogger) : IRequestHandler<RefundPaymentCommand, Unit>
{
    public async Task<Unit> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .Include(p => p.Refunds)
            .Include(p => p.Payout)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");

        if (payment.Status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
            throw new InvalidOperationException("Only a paid payment can be refunded.");

        // After the lawyer was paid: only within the refund window, and someone must cover their share.
        var afterPayout = payment.Payout is { Status: PayoutStatus.Released };
        if (afterPayout)
        {
            var policy = await RefundPolicy.GetAsync(db, cancellationToken);
            if (DateTime.UtcNow > RefundPolicy.DeadlineFor(payment.Payout!, policy))
                throw new InvalidOperationException(
                    $"The refund period ({policy.RefundWindowDays} days after the lawyer was paid) has ended.");
        }
        var bearer = afterPayout ? request.LawyerShareBearer ?? RefundBearer.Lawyer : (RefundBearer?)null;

        var alreadyRefunded = payment.Refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount);
        if (alreadyRefunded + request.Amount > payment.Total)
            throw new InvalidOperationException("Refund amount exceeds what remains to be refunded.");

        // A wallet-funded payment never touched the external gateway — the "gateway" for that
        // money was our own wallet ledger, so reversing it is a wallet credit, not a gateway call.
        var wasWalletFunded = payment.MethodDescription == "Wallet";
        string? gatewayRefundId = null;
        if (!wasWalletFunded)
        {
            var gatewayResult = await gateway.RefundAsync(
                payment.GatewayPaymentId ?? throw new InvalidOperationException("Payment has no gateway reference."),
                request.Amount, cancellationToken);
            gatewayRefundId = gatewayResult.GatewayRefundId;
        }

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Amount = request.Amount,
            Reason = request.Reason,
            Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
            LawyerShareBearer = bearer,
            Status = RefundStatus.Completed,
            GatewayRefundId = gatewayRefundId,
            CompletedAtUtc = DateTime.UtcNow,
        };
        db.Refunds.Add(refund);

        var lawyerShareAccount = bearer switch
        {
            RefundBearer.Lawyer => LedgerAccount.LawyerReceivable,
            RefundBearer.Platform => LedgerAccount.RefundLossExpense,
            _ => LedgerAccount.EscrowPayable,
        };
        var refundEntries = LedgerPostingService.PostRefund(payment, refund, wasWalletFunded, lawyerShareAccount);
        db.LedgerEntries.AddRange(refundEntries);

        if (bearer == RefundBearer.Lawyer && payment.Payout is { } paidPayout)
        {
            db.LawyerDebtEntries.Add(new LawyerDebtEntry
            {
                Id = Guid.NewGuid(),
                LawyerProfileId = paidPayout.LawyerProfileId,
                Kind = LawyerDebtEntryKind.RefundAfterPayout,
                Amount = refundEntries.Where(e => e.Account == LedgerAccount.LawyerReceivable && e.IsDebit).Sum(e => e.Amount),
                PaymentId = payment.Id,
                RefundId = refund.Id,
                Note = request.Reason.ToString(),
                CreatedByUserId = currentUser.UserId,
            });
        }

        // The lawyer's held share shrinks by exactly what the ledger took out of escrow, so the payout
        // that is eventually released always matches the books.
        // A suspended payout (lawyer deleted their account) is still in escrow too.
        if (payment.Payout is { Status: PayoutStatus.Held or PayoutStatus.Suspended } payout)
        {
            payout.Amount -= refundEntries.Where(e => e.Account == LedgerAccount.EscrowPayable && e.IsDebit).Sum(e => e.Amount);
        }

        if (wasWalletFunded)
        {
            var clientProfile = await db.ClientProfiles.FirstAsync(c => c.Id == payment.ClientId, cancellationToken);
            var wallet = await WalletAccessor.GetOrCreateAsync(db, clientProfile.UserId, cancellationToken);
            wallet.Balance += request.Amount;
            db.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = WalletTransactionType.Refund,
                Amount = request.Amount,
                PaymentId = payment.Id,
                Description = $"Refund: {request.Reason}" + (string.IsNullOrWhiteSpace(request.Details) ? "" : $" — {request.Details.Trim()}"),
            });
        }

        var totalRefunded = alreadyRefunded + request.Amount;
        payment.Status = totalRefunded >= payment.Total ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        if (payment.Status == PaymentStatus.Refunded && payment.Payout is { Status: PayoutStatus.Held or PayoutStatus.Suspended } fullyRefunded)
        {
            fullyRefunded.Status = PayoutStatus.Cancelled;
            fullyRefunded.Amount = 0;
        }

        if (payment.Status == PaymentStatus.Refunded && payment.ServiceRequestId is { } requestId)
        {
            var serviceRequest = await db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
            if (serviceRequest is not null)
                db.RequestStatusHistories.Add(serviceRequest.TransitionTo(RequestStatus.Refunded, "PaymentRefunded"));
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("PaymentRefunded", nameof(Payment), payment.Id.ToString(),
            $"{refund.Amount:0.00} SAR · {refund.Reason}{(refund.Details is null ? "" : " · " + refund.Details)}", cancellationToken);
        return Unit.Value;
    }
}
