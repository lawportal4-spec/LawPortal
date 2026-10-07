using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Onboarding;
using LawPortal.Application.Subscriptions;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments.Commands;

/// <summary>
/// Handles a gateway status callback. Idempotent by design — Moyasar (and every serious
/// gateway) can and does redeliver the same webhook, so a payment already in a terminal state
/// is a no-op rather than a second ledger posting. <paramref name="SecretToken"/> mirrors
/// Moyasar's documented webhook auth: the payload carries a shared secret the receiver compares
/// for equality, rather than a computed HMAC signature.
///
/// One gateway, one callback URL, two kinds of thing it might be settling — a marketplace
/// <see cref="Payment"/> or a <see cref="SubscriptionInvoice"/> (P10). Real gateways only ever
/// call back to the one URL configured for the whole merchant account, so this dispatches on
/// whichever record actually owns the gateway reference rather than needing two endpoints.
/// </summary>
public record HandleGatewayWebhookCommand(string GatewayPaymentId, string Status, string SecretToken) : IRequest<Unit>;

public class HandleGatewayWebhookHandler(ILawPortalDbContext db, Microsoft.Extensions.Configuration.IConfiguration configuration)
    : IRequestHandler<HandleGatewayWebhookCommand, Unit>
{
    public async Task<Unit> Handle(HandleGatewayWebhookCommand request, CancellationToken cancellationToken)
    {
        var expectedSecret = configuration["Payments:WebhookSecret"]
            ?? throw new InvalidOperationException("Payments:WebhookSecret is not configured.");
        if (request.SecretToken != expectedSecret)
            throw new UnauthorizedAccessException("Webhook secret token does not match.");

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.GatewayPaymentId == request.GatewayPaymentId, cancellationToken);

        if (payment is not null)
        {
            await HandlePaymentAsync(payment, request.Status, cancellationToken);
            return Unit.Value;
        }

        var feeInvoice = await db.RegistrationFeeInvoices
            .FirstOrDefaultAsync(i => i.GatewayPaymentId == request.GatewayPaymentId, cancellationToken);
        if (feeInvoice is not null)
        {
            await HandleRegistrationFeeAsync(feeInvoice, request.Status, cancellationToken);
            return Unit.Value;
        }

        var invoice = await db.SubscriptionInvoices
            .Include(i => i.LawyerSubscription)
            .FirstOrDefaultAsync(i => i.GatewayPaymentId == request.GatewayPaymentId, cancellationToken)
            ?? throw new KeyNotFoundException("No payment or invoice matches this gateway reference.");

        await HandleSubscriptionInvoiceAsync(invoice, request.Status, configuration, cancellationToken);
        return Unit.Value;
    }

    private async Task HandlePaymentAsync(Payment payment, string status, CancellationToken cancellationToken)
    {
        if (payment.Status is PaymentStatus.Paid or PaymentStatus.Failed)
            return; // Already settled — a replay must not re-post the ledger.

        var serviceRequest = payment.ServiceRequestId is { } requestId
            ? await db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            : null;

        if (status == "paid")
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAtUtc = DateTime.UtcNow;

            if (payment.Purpose == PaymentPurpose.WalletTopUp)
            {
                var clientProfile = await db.ClientProfiles.FirstAsync(c => c.Id == payment.ClientId, cancellationToken);
                var wallet = await Wallet.WalletAccessor.GetOrCreateAsync(db, clientProfile.UserId, cancellationToken);
                wallet.Balance += payment.Total;
                db.WalletTransactions.Add(new Domain.Wallet.WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = Domain.Wallet.WalletTransactionType.TopUp,
                    Amount = payment.Total,
                    PaymentId = payment.Id,
                    Description = "Wallet top-up",
                });
                db.LedgerEntries.AddRange(LedgerPostingService.PostWalletTopUp(payment));
            }
            else if (serviceRequest is not null)
            {
                db.LedgerEntries.AddRange(LedgerPostingService.PostCardCheckoutSuccess(payment));

                string? vatNumber = null;
                if (payment.LawyerProfileId is { } lawyerProfileId)
                {
                    var lawyer = await db.LawyerProfiles.FirstOrDefaultAsync(l => l.Id == lawyerProfileId, cancellationToken);
                    vatNumber = lawyer?.VatNumber;
                }

                await Commands.InitiateCheckoutHandler.FinalizeSuccessfulPaymentAsync(db, payment, serviceRequest, vatNumber, cancellationToken);
            }
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Gateway reported a failed payment (e.g. 3DS declined).";
            await DiscountService.ReleaseAsync(db, payment.Id, cancellationToken);
            // The request deliberately stays Submitted — the client can simply retry checkout.
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleRegistrationFeeAsync(LawyerRegistrationFeeInvoice invoice, string status, CancellationToken cancellationToken)
    {
        if (invoice.Status is not RegistrationFeeInvoiceStatus.Pending)
            return; // Already settled — a replay must not re-post the ledger.

        if (status == "paid")
        {
            invoice.Status = RegistrationFeeInvoiceStatus.Paid;
            invoice.PaidAtUtc = DateTime.UtcNow;
            invoice.QrPayloadBase64 = ZatcaQrCodeBuilder.Build(
                "بوابة القانون", configuration["Payments:PlatformVatNumber"] ?? "", invoice.PaidAtUtc.Value, invoice.Total, invoice.VatAmount);
            db.LedgerEntries.AddRange(LedgerPostingService.PostRegistrationFee(invoice));
            await DiscountService.ConfirmAsync(db, invoice.Id, cancellationToken);

            // The fee was the last onboarding step: the portal opens and the lawyer becomes bookable.
            var lawyer = await db.LawyerProfiles.Include(l => l.User).FirstAsync(l => l.Id == invoice.LawyerProfileId, cancellationToken);
            LawyerOnboarding.Activate(lawyer);
        }
        else
        {
            invoice.Status = RegistrationFeeInvoiceStatus.Failed;
            invoice.FailureReason = "Gateway reported a failed payment (e.g. 3DS declined).";
            await DiscountService.ReleaseAsync(db, invoice.Id, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleSubscriptionInvoiceAsync(
        SubscriptionInvoice invoice, string status, Microsoft.Extensions.Configuration.IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (invoice.Status is not SubscriptionInvoiceStatus.Pending)
            return; // Already settled — a replay must not re-post the ledger or re-extend the period.

        var subscription = invoice.LawyerSubscription
            ?? await db.LawyerSubscriptions.FirstAsync(s => s.Id == invoice.LawyerSubscriptionId, cancellationToken);

        if (status == "paid")
        {
            invoice.Status = SubscriptionInvoiceStatus.Paid;
            invoice.PaidAtUtc = DateTime.UtcNow;

            var platformVatNumber = configuration["Payments:PlatformVatNumber"] ?? "";
            invoice.QrPayloadBase64 = ZatcaQrCodeBuilder.Build("بوابة القانون", platformVatNumber, invoice.PaidAtUtc.Value, invoice.Total, invoice.VatAmount);

            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStartUtc = invoice.PeriodStartUtc;
            subscription.CurrentPeriodEndUtc = invoice.PeriodEndUtc;
            subscription.ConsecutiveFailedAttempts = 0;

            db.LedgerEntries.AddRange(LedgerPostingService.PostSubscriptionPayment(invoice));
            await DiscountService.ConfirmAsync(db, invoice.Id, cancellationToken);
        }
        else
        {
            invoice.Status = SubscriptionInvoiceStatus.Failed;
            invoice.FailureReason = "Gateway reported a failed payment (e.g. 3DS declined).";
            await DiscountService.ReleaseAsync(db, invoice.Id, cancellationToken);

            subscription.ConsecutiveFailedAttempts++;
            subscription.Status = subscription.ConsecutiveFailedAttempts >= SubscriptionBillingPolicy.MaxDunningAttempts
                ? SubscriptionStatus.Expired
                : SubscriptionStatus.PastDue;
            // If still under budget, SubscriptionRenewalService's next sweep generates the
            // retry invoice — the webhook only updates the outcome, never creates new invoices.
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
