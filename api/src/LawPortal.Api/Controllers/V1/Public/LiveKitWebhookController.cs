using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using LawPortal.Application.Calls.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LawPortal.Api.Controllers.V1.Public;

/// <summary>
/// Unauthenticated by necessity (the caller is the LiveKit server, not a logged-in user) —
/// authenticity instead rests on LiveKit's own webhook auth scheme: the request carries an
/// `Authorize` header containing a JWT signed with our API secret. Verifying that signature is
/// the security boundary here; this pass doesn't additionally verify LiveKit's optional
/// body-hash claim, which would be a defense-in-depth extra, not what actually authenticates
/// the caller.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/livekit")]
public class LiveKitWebhookController(ISender sender, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Authorize", out var authHeader) || string.IsNullOrEmpty(authHeader))
            return Unauthorized();

        var apiSecret = configuration["LiveKit:ApiSecret"]
            ?? throw new InvalidOperationException("LiveKit:ApiSecret is not configured.");

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(authHeader.ToString(), new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiSecret)),
            }, out _);
        }
        catch (Exception)
        {
            return Unauthorized();
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<LiveKitWebhookPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload?.Room?.Name is not { } roomName)
            return Ok(); // Events with no room (e.g. participant-level pings) don't apply to a session.

        await sender.Send(new HandleLiveKitWebhookCommand(payload.Event ?? "", roomName), cancellationToken);
        return Ok();
    }

    private record LiveKitWebhookPayload(string? Event, LiveKitRoom? Room);
    private record LiveKitRoom(string? Name);
}
