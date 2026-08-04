using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments.Dtos;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Payments.Commands;

public record InitiateWalletTopUpCommand(decimal Amount) : IRequest<CheckoutResultDto>;

public class InitiateWalletTopUpValidator : AbstractValidator<InitiateWalletTopUpCommand>
{
    public InitiateWalletTopUpValidator() => RuleFor(x => x.Amount).GreaterThan(0);
}

public class InitiateWalletTopUpHandler(
    ILawPortalDbContext db,
    ICurrentUser currentUser,
    IPaymentGateway gateway,
    IConfiguration configuration)
    : IRequestHandler<InitiateWalletTopUpCommand, CheckoutResultDto>
{
    public async Task<CheckoutResultDto> Handle(InitiateWalletTopUpCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Number = await PaymentNumberGenerator.NextPaymentNumberAsync(db, cancellationToken),
            Purpose = PaymentPurpose.WalletTopUp,
            ClientId = clientId,
            Total = request.Amount,
            CurrencyCode = "SAR",
            MethodDescription = "Card",
        };
        db.Payments.Add(payment);

        var baseUrl = configuration["Payments:PublicBaseUrl"] ?? "http://localhost:5280";
        var callbackUrl = $"{baseUrl}/api/v1/webhooks/payment-gateway";
        var result = await gateway.CreatePaymentAsync(
            payment.Id, payment.Total, payment.CurrencyCode, "Law Portal — Wallet top-up", callbackUrl, cancellationToken);

        payment.GatewayProvider = gateway.Name;
        payment.GatewayPaymentId = result.GatewayPaymentId;

        await db.SaveChangesAsync(cancellationToken);
        return new CheckoutResultDto(payment.Id, payment.Number, "Initiated", result.RedirectUrl, PaidImmediately: false);
    }
}
