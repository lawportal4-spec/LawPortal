using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments.Dtos;
using LawPortal.Application.Requests.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments.Queries;

public record GetInvoiceQuery(Guid RequestId) : IRequest<InvoiceDto>;

public class GetInvoiceHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetInvoiceQuery, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var owns = await db.ServiceRequests.AnyAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken);
        if (!owns) throw new KeyNotFoundException("Request not found.");

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.ServiceRequestId == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("No invoice has been issued for this request yet.");

        return new InvoiceDto(
            invoice.Number, invoice.SubtotalExVat, invoice.VatAmount, invoice.Total, invoice.IsVatApplicable,
            invoice.SellerNameAr, invoice.SellerNameEn, invoice.SellerVatNumber, invoice.QrPayloadBase64, invoice.IssuedAtUtc,
            invoice.DiscountAmount);
    }
}

public record GetPaymentStatusQuery(Guid PaymentId) : IRequest<PaymentSummaryDto>;

public class GetPaymentStatusHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetPaymentStatusQuery, PaymentSummaryDto>
{
    public async Task<PaymentSummaryDto> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");

        return new PaymentSummaryDto(payment.Id, payment.Number, payment.Status.ToString(), payment.Total, payment.CurrencyCode, payment.CreatedAtUtc, payment.PaidAtUtc);
    }
}
