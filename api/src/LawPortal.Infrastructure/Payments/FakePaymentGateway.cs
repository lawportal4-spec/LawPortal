using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Infrastructure.Payments;

/// <summary>
/// Development stand-in for a real gateway — no Moyasar account exists yet (an open question
/// since the plan's first draft). Unlike a hand-waved mock, this one genuinely round-trips
/// through our own webhook endpoint: <c>FakeGatewayController</c> posts a real HTTP request back
/// to <c>/api/v1/webhooks/payment-gateway</c> with the shared secret, exercising the exact same
/// idempotency and ledger-posting path a real Moyasar webhook would hit. Swap the DI registration
/// for <see cref="MoyasarPaymentGateway"/> once real credentials exist.
/// </summary>
public class FakePaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    public string Name => "Fake";

    public Task<GatewayCheckoutResult> CreatePaymentAsync(
        Guid paymentId, decimal amount, string currency, string description, string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Payments:PublicBaseUrl"] ?? "http://localhost:5280";
        var redirectUrl = $"{baseUrl}/api/v1/dev/fake-gateway/{paymentId}";
        return Task.FromResult(new GatewayCheckoutResult(paymentId.ToString(), "initiated", redirectUrl));
    }

    public Task<GatewayRefundResult> RefundAsync(string gatewayPaymentId, decimal amount, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GatewayRefundResult($"fake_refund_{Guid.NewGuid():N}", "refunded"));
}
