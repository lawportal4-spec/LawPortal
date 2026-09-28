using System.Text.Json.Serialization;
using LawPortal.Application.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Public;

/// <summary>Unauthenticated by necessity — the caller is the payment gateway, not a logged-in
/// user. Authenticity instead rests on <see cref="GatewayWebhookBody.SecretToken"/> matching our
/// configured value, which <c>HandleGatewayWebhookCommand</c> checks before touching anything.</summary>
[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Two body shapes arrive here and both have to keep working.
    ///
    /// Moyasar posts an <em>event envelope</em>: top-level <c>id</c> is the event's own id, there is
    /// no top-level <c>status</c>, and the thing that was paid sits under <c>data</c>. That matters
    /// more than it looks — Moyasar has no <c>invoice_paid</c> event, only <c>payment_*</c> ones, so
    /// <c>data.id</c> is a *payment* id while <c>Payment.GatewayPaymentId</c> holds the *invoice* id
    /// (<c>MoyasarPaymentGateway</c> creates a hosted invoice, not a bare payment). The field that
    /// actually ties the two together is <c>data.invoice_id</c>. Reading top-level <c>id</c> here
    /// would look up an event id, match nothing, and leave every payment stuck at Initiated.
    ///
    /// <c>FakeGatewayController</c> posts the flat <c>{id, status, secret_token}</c> shape with no
    /// <c>data</c> at all. The coalescing chain below resolves both, so the handler, the fake
    /// gateway and the subscription path never need to know which one turned up.
    /// </summary>
    public record GatewayWebhookBody(
        string? Id,
        string? Status,
        [property: JsonPropertyName("secret_token")] string SecretToken,
        GatewayWebhookData? Data = null)
    {
        /// <summary>The value to match against <c>GatewayPaymentId</c> on a Payment or SubscriptionInvoice.</summary>
        public string? GatewayReference => Data?.InvoiceId ?? Data?.Id ?? Id;

        public string? ResolvedStatus => Data?.Status ?? Status;
    }

    public record GatewayWebhookData(
        string? Id,
        string? Status,
        [property: JsonPropertyName("invoice_id")] string? InvoiceId);

    [HttpPost("payment-gateway")]
    public async Task<IActionResult> PaymentGateway(GatewayWebhookBody body, CancellationToken cancellationToken)
    {
        if (body.GatewayReference is not { } reference || body.ResolvedStatus is not { } status)
            return BadRequest("Webhook body carried no gateway reference or status.");

        await sender.Send(new HandleGatewayWebhookCommand(reference, status, body.SecretToken), cancellationToken);
        return Ok();
    }
}
