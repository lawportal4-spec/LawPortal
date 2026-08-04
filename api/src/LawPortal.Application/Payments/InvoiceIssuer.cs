using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Payments;

namespace LawPortal.Application.Payments;

public static class InvoiceIssuer
{
    public static async Task<Invoice> IssueAsync(ILawPortalDbContext db, Payment payment, string? sellerVatNumber, CancellationToken cancellationToken)
    {
        var number = await PaymentNumberGenerator.NextInvoiceNumberAsync(db, cancellationToken);
        var issuedAt = DateTime.UtcNow;

        // No ZATCA QR is required when the underlying supply isn't VAT-registered — there is
        // no taxable seller for the code to describe.
        var qr = payment.IsVatApplicable
            ? ZatcaQrCodeBuilder.Build("بوابة القانون", sellerVatNumber ?? "", issuedAt, payment.Total, payment.VatAmount)
            : "";

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            ServiceRequestId = payment.ServiceRequestId
                ?? throw new InvalidOperationException("Cannot invoice a payment with no associated request."),
            Number = number,
            SubtotalExVat = payment.Total - payment.VatAmount,
            VatAmount = payment.VatAmount,
            Total = payment.Total,
            IsVatApplicable = payment.IsVatApplicable,
            SellerVatNumber = payment.IsVatApplicable ? sellerVatNumber : null,
            QrPayloadBase64 = qr,
            IssuedAtUtc = issuedAt,
        };
        db.Invoices.Add(invoice);
        return invoice;
    }
}
