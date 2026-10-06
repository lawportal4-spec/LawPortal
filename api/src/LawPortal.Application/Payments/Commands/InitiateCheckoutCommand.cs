using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments.Dtos;
using LawPortal.Application.Requests.Commands;
using LawPortal.Application.Wallet;
using LawPortal.Domain.Chat;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Payments.Commands;

public record InitiateCheckoutCommand(Guid RequestId, string PaymentMethod, string? DiscountCode = null) : IRequest<CheckoutResultDto>;

public class InitiateCheckoutValidator : AbstractValidator<InitiateCheckoutCommand>
{
    public InitiateCheckoutValidator() =>
        RuleFor(x => x.PaymentMethod).Must(m => m is "Card" or "Wallet")
            .WithMessage("PaymentMethod must be 'Card' or 'Wallet'.");
}

public class InitiateCheckoutHandler(
    ILawPortalDbContext db,
    ICurrentUser currentUser,
    IPaymentGateway gateway,
    IConfiguration configuration)
    : IRequestHandler<InitiateCheckoutCommand, CheckoutResultDto>
{
    public async Task<CheckoutResultDto> Handle(InitiateCheckoutCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var serviceRequest = await db.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        // A BiddingRequest becomes checkout-eligible once an offer is accepted (Awarded), not at
        // Submitted — there's no known price (and so no known lawyer to pay) until then.
        var expectedStatus = serviceRequest is BiddingRequest ? RequestStatus.Awarded : RequestStatus.Submitted;
        if (serviceRequest.Status != expectedStatus)
            throw new InvalidOperationException($"Only a {expectedStatus} request can be checked out.");

        if (serviceRequest.Subtotal is not { } amount)
            throw new InvalidOperationException("This request does not have a price yet and cannot be checked out.");

        var lawyer = await AssignedLawyerAsync(db, serviceRequest, cancellationToken);
        Guid? lawyerProfileId = lawyer?.Id;
        Guid? assignedLawyerId = lawyerProfileId;
        bool isVatApplicable = lawyer?.IsVatRegistered ?? false;
        string? vatNumber = lawyer?.VatNumber;

        // A category-specific CommissionPolicy override is modeled but nothing sets one yet;
        // a subscribed lawyer's own plan-level discount (P10) takes precedence when one applies.
        var commissionPercentage = await CommissionPolicyResolver.ResolvePercentageAsync(db, categorySlug: null, assignedLawyerId, cancellationToken);
        var paymentId = Guid.NewGuid();
        DiscountEvaluation? discount = null;
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
            discount = await DiscountService.ReserveAsync(db, request.DiscountCode, DiscountService.ScopeOf(serviceRequest),
                currentUser.UserId!.Value, amount, paymentId, cancellationToken);
        var discountAmount = discount?.Amount ?? 0;
        // Platform-funded: the lawyer's share and commission stay on the full price.
        var breakdown = PaymentBreakdownCalculator.ComputeWithPlatformDiscount(amount, discountAmount, isVatApplicable, commissionPercentage);

        var payment = new Domain.Payments.Payment
        {
            Id = paymentId,
            Number = await PaymentNumberGenerator.NextPaymentNumberAsync(db, cancellationToken),
            Purpose = PaymentPurpose.RequestCheckout,
            ServiceRequestId = serviceRequest.Id,
            ClientId = clientId,
            LawyerProfileId = lawyerProfileId,
            GrossAmount = amount,
            DiscountAmount = discountAmount,
            DiscountCodeId = discount?.Code?.Id,
            Total = breakdown.Total,
            VatAmount = breakdown.VatAmount,
            CommissionAmount = breakdown.CommissionAmount,
            NetToLawyerAmount = breakdown.NetToLawyerAmount,
            IsVatApplicable = breakdown.IsVatApplicable,
            CurrencyCode = serviceRequest.CurrencyCode,
            MethodDescription = request.PaymentMethod,
        };
        db.Payments.Add(payment);

        if (request.PaymentMethod == "Wallet")
        {
            var clientProfile = await db.ClientProfiles.FirstAsync(c => c.Id == clientId, cancellationToken);
            var wallet = await WalletAccessor.GetOrCreateAsync(db, clientProfile.UserId, cancellationToken);
            if (wallet.Balance < payment.Total)
                throw new InvalidOperationException("Insufficient wallet balance.");

            wallet.Balance -= payment.Total;
            db.WalletTransactions.Add(new Domain.Wallet.WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = Domain.Wallet.WalletTransactionType.Payment,
                Amount = payment.Total,
                PaymentId = payment.Id,
                Description = $"Payment for request {serviceRequest.Number}",
            });

            payment.Status = PaymentStatus.Paid;
            payment.PaidAtUtc = DateTime.UtcNow;
            payment.GatewayProvider = "Wallet";

            db.LedgerEntries.AddRange(LedgerPostingService.PostWalletCheckoutSuccess(payment));
            await FinalizeSuccessfulPaymentAsync(db, payment, serviceRequest, vatNumber, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            return new CheckoutResultDto(payment.Id, payment.Number, "Paid", null, PaidImmediately: true);
        }

        var baseUrl = configuration["Payments:PublicBaseUrl"] ?? "http://localhost:5280";
        var callbackUrl = $"{baseUrl}/api/v1/webhooks/payment-gateway";
        var result = await gateway.CreatePaymentAsync(
            payment.Id, payment.Total, payment.CurrencyCode,
            $"Law Portal — {serviceRequest.Number}", callbackUrl, cancellationToken);

        payment.GatewayProvider = gateway.Name;
        payment.GatewayPaymentId = result.GatewayPaymentId;

        await db.SaveChangesAsync(cancellationToken);
        return new CheckoutResultDto(payment.Id, payment.Number, "Initiated", result.RedirectUrl, PaidImmediately: false);
    }

    /// <summary>The lawyer who'll be paid. CatalogRequest has no lawyer assigned at checkout time
    /// (see Payment.LawyerProfileId doc), so it has no known VAT registration and checks out VAT-free.</summary>
    internal static async Task<Domain.Identity.LawyerProfile?> AssignedLawyerAsync(
        ILawPortalDbContext db, ServiceRequest serviceRequest, CancellationToken cancellationToken)
    {
        Guid? assignedLawyerId = serviceRequest switch
        {
            ConsultationRequest consultation => consultation.LawyerProfileId,
            BiddingRequest bidding => bidding.AwardedLawyerProfileId,
            _ => null,
        };
        if (assignedLawyerId is not { } knownLawyerId) return null;
        return await db.LawyerProfiles.FirstOrDefaultAsync(l => l.Id == knownLawyerId, cancellationToken)
            ?? throw new InvalidOperationException("The assigned lawyer no longer exists.");
    }

    internal static async Task FinalizeSuccessfulPaymentAsync(
        ILawPortalDbContext db, Domain.Payments.Payment payment, ServiceRequest serviceRequest, string? sellerVatNumber, CancellationToken cancellationToken)
    {
        db.RequestStatusHistories.Add(serviceRequest.TransitionTo(RequestStatus.Paid, "PaymentSucceeded"));

        if (payment.LawyerProfileId is { } lawyerProfileId)
        {
            db.Payouts.Add(new Domain.Payments.Payout
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                LawyerProfileId = lawyerProfileId,
                Amount = payment.NetToLawyerAmount,
            });
        }

        // Chat opens automatically once a request is paid and has a known lawyer to talk
        // to — consultations (always) and now-awarded bidding requests (this pass). Catalog
        // requests (notarization/trademark) never get one, matching MessageThread's docs.
        Guid? chatLawyerProfileId = serviceRequest switch
        {
            ConsultationRequest consultation => consultation.LawyerProfileId,
            BiddingRequest bidding => bidding.AwardedLawyerProfileId,
            _ => null,
        };
        if (chatLawyerProfileId is { } lawyerProfileIdForChat)
        {
            var clientProfile = await db.ClientProfiles.FirstAsync(c => c.Id == serviceRequest.ClientId, cancellationToken);
            var lawyerProfile = await db.LawyerProfiles.FirstAsync(l => l.Id == lawyerProfileIdForChat, cancellationToken);
            db.MessageThreads.Add(new MessageThread
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = serviceRequest.Id,
                ClientUserId = clientProfile.UserId,
                LawyerUserId = lawyerProfile.UserId,
            });
        }

        await DiscountService.ConfirmAsync(db, payment.Id, cancellationToken);
        await InvoiceIssuer.IssueAsync(db, payment, sellerVatNumber, cancellationToken);
    }
}
