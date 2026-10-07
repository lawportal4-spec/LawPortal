using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Payments;
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

    /// <summary>Realistic-looking, stable sample details (derived from the id) so the admin page can be
    /// built and checked without a Moyasar account. Clearly fake: the card is a test BIN.</summary>
    public Task<GatewayTransaction?> GetTransactionAsync(string gatewayPaymentId, CancellationToken cancellationToken = default)
    {
        var seed = (uint)gatewayPaymentId.GetHashCode();
        var brands = new[] { "mada", "visa", "master" };
        return Task.FromResult<GatewayTransaction?>(new GatewayTransaction
        {
            TransactionId = $"fake_pay_{gatewayPaymentId.Replace("-", "")[..12]}",
            SourceType = "creditcard",
            CardBrand = brands[seed % 3],
            CardMasked = $"4201-32XX-XXXX-{seed % 10000:D4}",
            ReferenceNumber = $"{seed % 1_000_000_000_000:D12}",
            AuthorizationCode = $"{seed % 1_000_000:D6}",
            ResponseCode = "00",
            Message = "APPROVED",
            Fee = null,
            FetchedAtUtc = DateTime.UtcNow,
        });
    }
}
