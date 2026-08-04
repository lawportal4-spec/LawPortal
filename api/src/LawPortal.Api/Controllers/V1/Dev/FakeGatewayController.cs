using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LawPortal.Api.Controllers.V1.Dev;

/// <summary>
/// Stands in for a real hosted checkout page. Confirming genuinely round-trips through
/// <c>WebhooksController</c> over real HTTP — same signature check, same idempotency path a
/// live Moyasar webhook would hit — so "replayed webhook doesn't double-credit" and "failed 3DS
/// leaves the user coherent" are provably true, not just asserted. Registered unconditionally
/// but refuses to serve outside Development.
/// </summary>
[ApiController]
[Route("api/v1/dev/fake-gateway")]
public class FakeGatewayController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHostEnvironment environment)
    : ControllerBase
{
    [HttpGet("{paymentId:guid}")]
    public IActionResult ConfirmationPage(Guid paymentId)
    {
        if (!environment.IsDevelopment()) return NotFound();

        var html = $"""
            <html><body style="font-family: sans-serif; max-width: 480px; margin: 60px auto;">
              <h2>Fake gateway — {paymentId}</h2>
              <p>Development-only checkout simulator. Pick an outcome:</p>
              <form method="post" action="/api/v1/dev/fake-gateway/{paymentId}/confirm?outcome=success">
                <button type="submit" style="padding: 10px 20px; margin-inline-end: 8px;">Pay successfully</button>
              </form>
              <form method="post" action="/api/v1/dev/fake-gateway/{paymentId}/confirm?outcome=fail">
                <button type="submit" style="padding: 10px 20px;">Simulate 3DS failure</button>
              </form>
            </body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpPost("{paymentId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid paymentId, [FromQuery] string outcome, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();

        var secret = configuration["Payments:WebhookSecret"]
            ?? throw new InvalidOperationException("Payments:WebhookSecret is not configured.");
        var baseUrl = configuration["Payments:PublicBaseUrl"] ?? "http://localhost:5280";

        var client = httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync($"{baseUrl}/api/v1/webhooks/payment-gateway", new
        {
            id = paymentId.ToString(),
            status = outcome == "success" ? "paid" : "failed",
            secret_token = secret,
        }, cancellationToken);

        return Content(
            $"Webhook call returned {(int)response.StatusCode}. Outcome simulated: {outcome}.",
            "text/plain");
    }
}
