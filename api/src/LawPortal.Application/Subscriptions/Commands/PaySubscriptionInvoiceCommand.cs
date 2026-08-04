using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Payments.Dtos;
using LawPortal.Domain.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Subscriptions.Commands;

/// <summary>Card-only — lawyers have no wallet in this system (a wallet funds a client's own
/// checkouts; a lawyer's money moves the other way, via payout). Covers a brand-new
/// subscription's first invoice and every renewal/dunning-retry invoice through the same call,
/// going through the identical <c>IPaymentGateway</c> + webhook flow every other payment here
/// uses.</summary>
public record PaySubscriptionInvoiceCommand(Guid InvoiceId) : IRequest<CheckoutResultDto>;

public class PaySubscriptionInvoiceHandler(
    ILawPortalDbContext db, ICurrentUser currentUser, IPaymentGateway gateway, IConfiguration configuration)
    : IRequestHandler<PaySubscriptionInvoiceCommand, CheckoutResultDto>
{
    public async Task<CheckoutResultDto> Handle(PaySubscriptionInvoiceCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var invoice = await db.SubscriptionInvoices
            .Include(i => i.LawyerSubscription)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.LawyerSubscription!.LawyerProfileId == lawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.Status != SubscriptionInvoiceStatus.Pending)
            throw new InvalidOperationException($"This invoice is {invoice.Status} and cannot be paid.");

        var baseUrl = configuration["Payments:PublicBaseUrl"] ?? "http://localhost:5280";
        var callbackUrl = $"{baseUrl}/api/v1/webhooks/payment-gateway";
        var result = await gateway.CreatePaymentAsync(
            invoice.Id, invoice.Total, "SAR", $"Law Portal — subscription {invoice.Number}", callbackUrl, cancellationToken);

        invoice.GatewayProvider = gateway.Name;
        invoice.GatewayPaymentId = result.GatewayPaymentId;

        await db.SaveChangesAsync(cancellationToken);
        return new CheckoutResultDto(invoice.Id, invoice.Number, "Initiated", result.RedirectUrl, PaidImmediately: false);
    }
}
