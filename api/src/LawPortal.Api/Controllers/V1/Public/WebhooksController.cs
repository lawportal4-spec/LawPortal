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
    public record GatewayWebhookBody(
        string Id,
        string Status,
        [property: JsonPropertyName("secret_token")] string SecretToken);

    [HttpPost("payment-gateway")]
    public async Task<IActionResult> PaymentGateway(GatewayWebhookBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new HandleGatewayWebhookCommand(body.Id, body.Status, body.SecretToken), cancellationToken);
        return Ok();
    }
}
