namespace LawPortal.Application.Common.Interfaces;

public record GatewayCheckoutResult(string GatewayPaymentId, string Status, string? RedirectUrl);

public record GatewayRefundResult(string GatewayRefundId, string Status);

/// <summary>
/// Card/wallet checkout against an external payment provider. <c>MoyasarPaymentGateway</c> is
/// the real implementation, written against Moyasar's public API docs but never exercised
/// against a live account — this project has no Moyasar merchant credentials yet (flagged as an
/// open question since the plan's first draft). <c>FakePaymentGateway</c> is the Development
/// stand-in that exercises the full checkout → redirect → webhook → ledger path for real,
/// against our own process, so that path can be built and verified without one.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>"Moyasar" or "Fake" — stamped onto <see cref="LawPortal.Domain.Payments.Payment.GatewayProvider"/>
    /// so it's visible in the data which path a given payment actually took.</summary>
    string Name { get; }

    Task<GatewayCheckoutResult> CreatePaymentAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string description,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    Task<GatewayRefundResult> RefundAsync(
        string gatewayPaymentId,
        decimal amount,
        CancellationToken cancellationToken = default);
}
