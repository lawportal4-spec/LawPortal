using LawPortal.Application.Auth.Commands;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LawPortal.Api.Controllers.V1.Shared;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    public record RefreshBody(string RefreshToken);

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshBody body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTokenCommand(body.RefreshToken), cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(body.RefreshToken), cancellationToken);
        return NoContent();
    }

    /// <summary>The client accepts the "أتعهد…" pledge shown until they do (see MeDto.PledgeAccepted).</summary>
    [Authorize]
    [HttpPost("me/pledge")]
    public async Task<IActionResult> AcceptPledge(CancellationToken cancellationToken)
    {
        await sender.Send(new AcceptClientPledgeCommand(), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MeDto>> Me(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMeQuery(), cancellationToken);
        return Ok(result);
    }
}
