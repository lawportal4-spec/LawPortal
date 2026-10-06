using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Payments.Commands;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments.Queries;

/// <summary>What the payer will be charged for <paramref name="ReferenceId"/> (a client's request, or a
/// lawyer's subscription invoice), with <paramref name="Code"/> applied when given. Price and scope
/// always come from the referenced record, never from the caller.</summary>
public record PreviewCheckoutQuery(Guid ReferenceId, string? Code) : IRequest<CheckoutPreviewDto>;

/// <param name="Rejection">A <see cref="DiscountRejection"/> name when the code can't be used; the
/// figures are then the undiscounted ones.</param>
public record CheckoutPreviewDto(string Scope, decimal Gross, decimal Discount, decimal VatAmount, decimal Total, string? Rejection);

public class PreviewCheckoutHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<PreviewCheckoutQuery, CheckoutPreviewDto>
{
    public async Task<CheckoutPreviewDto> Handle(PreviewCheckoutQuery request, CancellationToken cancellationToken)
    {
        var (scope, gross, isVatApplicable) = currentUser.UserType == UserType.Lawyer
            ? await SubscriptionInvoiceAsync(request.ReferenceId, cancellationToken)
            : await ServiceRequestAsync(request.ReferenceId, cancellationToken);

        DiscountEvaluation? evaluation = null;
        if (!string.IsNullOrWhiteSpace(request.Code))
            evaluation = await DiscountService.EvaluateAsync(db, request.Code, scope, currentUser.UserId!.Value, gross, cancellationToken);

        var discount = evaluation?.Amount ?? 0;
        var total = gross - discount;
        var vat = isVatApplicable ? Math.Round(total * PaymentBreakdownCalculator.VatRate / (1 + PaymentBreakdownCalculator.VatRate), 2) : 0m;
        return new CheckoutPreviewDto(scope.ToString(), gross, discount, vat, total, evaluation?.Rejection?.ToString());
    }

    private async Task<(DiscountScope, decimal, bool)> ServiceRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);
        var serviceRequest = await db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");
        if (serviceRequest.Subtotal is not { } amount)
            throw new InvalidOperationException("This request does not have a price yet.");
        var lawyer = await InitiateCheckoutHandler.AssignedLawyerAsync(db, serviceRequest, cancellationToken);
        return (DiscountService.ScopeOf(serviceRequest), amount, lawyer?.IsVatRegistered ?? false);
    }

    private async Task<(DiscountScope, decimal, bool)> SubscriptionInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var invoice = await db.SubscriptionInvoices
            .FirstOrDefaultAsync(i => i.Id == id && i.LawyerSubscription!.LawyerProfileId == lawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");
        return (DiscountScope.LawyerSubscription, invoice.Total + invoice.DiscountAmount, true);
    }
}
