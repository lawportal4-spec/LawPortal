using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Wallet;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using LawPortal.Domain.Wallet;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments.Commands;

public record RefundPaymentCommand(Guid PaymentId, decimal Amount, string Reason) : IRequest<Unit>;

public class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RefundPaymentHandler(ILawPortalDbContext db, IPaymentGateway gateway) : IRequestHandler<RefundPaymentCommand, Unit>
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

        if (payment.Payout is { Status: PayoutStatus.Released })
            throw new InvalidOperationException(
                "This payment's escrow has already been released to the lawyer — refunding after payout needs a clawback process this pass doesn't build.");

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
            Status = RefundStatus.Completed,
            GatewayRefundId = gatewayRefundId,
            CompletedAtUtc = DateTime.UtcNow,
        };
        db.Refunds.Add(refund);

        db.LedgerEntries.AddRange(LedgerPostingService.PostRefund(payment, refund, wasWalletFunded));

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
                Description = $"Refund: {request.Reason}",
            });
        }

        var totalRefunded = alreadyRefunded + request.Amount;
        payment.Status = totalRefunded >= payment.Total ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

        if (payment.Status == PaymentStatus.Refunded && payment.ServiceRequestId is { } requestId)
        {
            var serviceRequest = await db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
            if (serviceRequest is not null)
                db.RequestStatusHistories.Add(serviceRequest.TransitionTo(RequestStatus.Refunded, "PaymentRefunded"));
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
