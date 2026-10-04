using LawPortal.Application.Auth.Commands;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LawPortal.Api.Controllers.V1.Lawyer;

[ApiController]
[Route("api/v1/auth/lawyer")]
[EnableRateLimiting("auth")]
public class LawyerAuthController(ISender sender) : ControllerBase
{
    /// <summary>multipart/form-data: the wizard's fields plus the licence scan in one request,
    /// since there is no signed-in user yet to hand a presigned upload URL to.</summary>
    public record RegisterForm(
        string FullName,
        string PhoneE164,
        string Email,
        string Password,
        int RegionId,
        int CityId,
        LawyerLicenseType LicenseType,
        string LicenseNumber,
        DateOnly IssueDate,
        DateOnly ExpiryDate,
        string CountryCode,
        bool AcceptedTerms,
        IFormFile? LicenseDocument,
        string RecaptchaToken);

    public record PhoneCodeBody(string PhoneE164, string Code);
    public record PhoneBody(string PhoneE164);

    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    // Kestrel's default body cap is far above 3MB; this just stops oversized uploads early.
    [RequestSizeLimit(4 * 1024 * 1024)]
    [ProducesResponseType<LawyerRegistrationStartedDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerRegistrationStartedDto>> Register([FromForm] RegisterForm form, CancellationToken cancellationToken)
    {
        await using var content = form.LicenseDocument?.OpenReadStream();
        var document = form.LicenseDocument is { } file
            ? new LicenseDocumentUpload(content!, file.FileName, file.ContentType, file.Length)
            : null;

        var result = await sender.Send(new RegisterLawyerCommand(
            form.FullName, form.PhoneE164, form.Email, form.Password, form.RegionId, form.CityId,
            form.LicenseType, form.LicenseNumber, form.IssueDate, form.ExpiryDate, form.CountryCode,
            form.AcceptedTerms, document, form.RecaptchaToken), cancellationToken);
        return Ok(result);
    }

    [HttpPost("register/verify")]
    public async Task<IActionResult> VerifyRegistration(PhoneCodeBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new VerifyLawyerRegistrationCommand(body.PhoneE164, body.Code), cancellationToken);
        return NoContent();
    }

    [HttpPost("register/resend")]
    public async Task<IActionResult> ResendRegistrationCode(PhoneBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new ResendLawyerRegistrationOtpCommand(body.PhoneE164), cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResultDto>> Login(LawyerLoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
