using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Infrastructure.Payments;

/// <summary>
/// Real Moyasar integration against their Invoices endpoint — a hosted checkout page, which fits
/// this project's redirect-based <see cref="IPaymentGateway"/> shape and keeps card data (and so
/// PCI scope) out of our frontend entirely, without needing Moyasar.js tokenization yet.
///
/// The thing to keep in mind when reading this class: we create an <em>invoice</em> and store its
/// id, but Moyasar's webhooks and refunds are <em>payment</em>-scoped. A hosted invoice spawns a
/// payment when the customer pays it, and <c>payment.invoice_id</c> is the only link back. That
/// asymmetry is why <see cref="RefundAsync"/> does a lookup first, and why
/// <c>WebhooksController</c> reads <c>data.invoice_id</c> rather than the id at the top of the body.
/// </summary>
public class MoyasarPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _client;
    private readonly string _publicBaseUrl;

    public string Name => "Moyasar";

    public MoyasarPaymentGateway(IConfiguration configuration)
    {
        var secretKey = configuration["Payments:Moyasar:SecretKey"]
            ?? throw new InvalidOperationException("Payments:Moyasar:SecretKey is not configured.");

        _publicBaseUrl = (configuration["Payments:PublicBaseUrl"] ?? "http://localhost:5280").TrimEnd('/');

        _client = new HttpClient { BaseAddress = new Uri("https://api.moyasar.com/v1/") };
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
    }

    public async Task<GatewayCheckoutResult> CreatePaymentAsync(
        Guid paymentId, decimal amount, string currency, string description, string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        // callback_url is Moyasar's server-to-server POST; success_url/back_url are where the
        // customer's *browser* lands once they're done. Without the latter two they finish paying
        // and simply sit on Moyasar's page. Both point at our own redirector, which knows how to
        // map a payment id back to the right page in the right app — see PaymentsReturnController.
        var returnUrl = $"{_publicBaseUrl}/api/v1/payments/{paymentId}/return";

        var response = await _client.PostAsJsonAsync("invoices", new
        {
            amount = ToHalalas(amount),
            currency,
            description,
            callback_url = callbackUrl,
            success_url = returnUrl,
            back_url = returnUrl,
            // Moyasar metadata values must be strings. Purely for traceability from their
            // dashboard back to our row — correlation in code goes through invoice_id.
            metadata = new { payment_id = paymentId.ToString() },
        }, cancellationToken);
        await EnsureSuccessAsync(response, "create invoice", cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<MoyasarInvoiceResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Moyasar returned an empty invoice response.");

        return new GatewayCheckoutResult(body.Id, body.Status, body.Url);
    }

    public async Task<GatewayRefundResult> RefundAsync(string gatewayPaymentId, decimal amount, CancellationToken cancellationToken = default)
    {
        // gatewayPaymentId is the invoice id stored at checkout. Posting it straight to
        // payments/{id}/refund is a guaranteed 404 — resolve the settled payment on it first.
        var moyasarPaymentId = await ResolveSettledPaymentIdAsync(gatewayPaymentId, cancellationToken);

        var response = await _client.PostAsJsonAsync(
            $"payments/{moyasarPaymentId}/refund", new { amount = ToHalalas(amount) }, cancellationToken);
        await EnsureSuccessAsync(response, "refund payment", cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<MoyasarRefundResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Moyasar returned an empty refund response.");

        return new GatewayRefundResult(body.Id, body.Status);
    }

    private async Task<string> ResolveSettledPaymentIdAsync(string invoiceId, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"invoices/{invoiceId}", cancellationToken);
        await EnsureSuccessAsync(response, "retrieve invoice", cancellationToken);

        var invoice = await response.Content.ReadFromJsonAsync<MoyasarInvoiceDetail>(cancellationToken)
            ?? throw new InvalidOperationException("Moyasar returned an empty invoice response.");

        var settled = invoice.Payments?.FirstOrDefault(p => p.Status is "paid" or "captured")
            ?? throw new InvalidOperationException(
                $"Moyasar invoice {invoiceId} has no settled payment to refund.");

        return settled.Id;
    }

    /// <summary>
    /// <c>EnsureSuccessStatusCode()</c> throws away the response body, which is exactly the part
    /// worth reading — Moyasar returns the offending field names in its 4xx payloads.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Moyasar {operation} failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }

    private static int ToHalalas(decimal amountSar) => (int)Math.Round(amountSar * 100m, MidpointRounding.AwayFromZero);

    private record MoyasarInvoiceResponse(string Id, string Status, string Url);
    private record MoyasarInvoiceDetail(string Id, string Status, IReadOnlyList<MoyasarInvoicePayment>? Payments);
    private record MoyasarInvoicePayment(string Id, string Status);
    private record MoyasarRefundResponse(string Id, string Status);
}
