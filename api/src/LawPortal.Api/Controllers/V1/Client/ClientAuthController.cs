using LawPortal.Application.Auth.Commands;
using LawPortal.Application.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LawPortal.Api.Controllers.V1.Client;

[ApiController]
[Route("api/v1/auth/client")]
[EnableRateLimiting("auth")]
public class ClientAuthController(ISender sender) : ControllerBase
{
    public record RequestOtpBody(string PhoneE164, string RecaptchaToken);
    public record VerifyOtpBody(string PhoneE164, string Code);

    [HttpPost("otp/request")]
    public async Task<IActionResult> RequestOtp(RequestOtpBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RequestClientOtpCommand(body.PhoneE164, body.RecaptchaToken), cancellationToken);
        return NoContent();
    }

    [HttpPost("otp/verify")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResultDto>> VerifyOtp(VerifyOtpBody body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyClientOtpCommand(body.PhoneE164, body.Code), cancellationToken);
        return Ok(result);
    }
}
