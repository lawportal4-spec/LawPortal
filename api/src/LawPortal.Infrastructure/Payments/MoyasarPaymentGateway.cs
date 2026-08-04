using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Infrastructure.Payments;

/// <summary>
/// Real Moyasar integration, written against their public API docs (Invoices endpoint — a
/// hosted checkout page, which fits this project's redirect-based <see cref="IPaymentGateway"/>
/// shape without needing Moyasar.js card tokenization in the frontend yet). This project has no
/// Moyasar merchant account, so this class has never been exercised against a live endpoint —
/// unlike <c>S3FileStorage</c>/<c>ClamAvVirusScanner</c>, which run against real local
/// containers, there is no local stand-in for a payment processor. Treat this as
/// implemented-to-spec, not verified, until real sandbox credentials exist.
/// </summary>
public class MoyasarPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _client;

    public string Name => "Moyasar";

    public MoyasarPaymentGateway(IConfiguration configuration)
    {
        var secretKey = configuration["Payments:Moyasar:SecretKey"]
            ?? throw new InvalidOperationException("Payments:Moyasar:SecretKey is not configured.");

        _client = new HttpClient { BaseAddress = new Uri("https://api.moyasar.com/v1/") };
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
    }

    public async Task<GatewayCheckoutResult> CreatePaymentAsync(
        Guid paymentId, decimal amount, string currency, string description, string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync("invoices", new
        {
            amount = ToHalalas(amount),
            currency,
            description,
            callback_url = callbackUrl,
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<MoyasarInvoiceResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Moyasar returned an empty invoice response.");

        return new GatewayCheckoutResult(body.Id, body.Status, body.Url);
    }

    public async Task<GatewayRefundResult> RefundAsync(string gatewayPaymentId, decimal amount, CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            $"payments/{gatewayPaymentId}/refund", new { amount = ToHalalas(amount) }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<MoyasarRefundResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Moyasar returned an empty refund response.");

        return new GatewayRefundResult(body.Id, body.Status);
    }

    private static int ToHalalas(decimal amountSar) => (int)Math.Round(amountSar * 100m, MidpointRounding.AwayFromZero);

    private record MoyasarInvoiceResponse(string Id, string Status, string Url);
    private record MoyasarRefundResponse(string Id, string Status);
}
