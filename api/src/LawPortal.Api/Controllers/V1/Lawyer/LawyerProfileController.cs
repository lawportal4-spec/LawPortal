using LawPortal.Application.Auth.Commands;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Domain.Identity;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Lawyers.Queries;
using LawPortal.Application.Lawyers.Onboarding;
using LawPortal.Application.Payments.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Lawyer;

[ApiController]
[Authorize(Roles = "Lawyer")]
[Route("api/v1/lawyer")]
public class LawyerProfileController(ISender sender) : ControllerBase
{
    public record PricingBody(decimal WrittenPrice, decimal Price15, decimal Price30, decimal Price45);
    public record ProfileBody(string? BioAr, string? BioEn, bool AcceptingNewRequests, IReadOnlyList<int> SpecialtyIds, IReadOnlyList<int> LanguageIds);
    public record RenewLicenseBody(string LicenseNumber, DateOnly IssueDate, DateOnly ExpiryDate);

    /// <summary>multipart/form-data — answers "returned for correction"; the document is optional
    /// unless the admin flagged the file.</summary>
    public record ResubmitLicenseForm(LawyerLicenseType LicenseType, string LicenseNumber, DateOnly IssueDate, DateOnly ExpiryDate, IFormFile? LicenseDocument);

    [HttpPost("license/resubmit")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> ResubmitLicense([FromForm] ResubmitLicenseForm form, CancellationToken cancellationToken)
    {
        await using var content = form.LicenseDocument?.OpenReadStream();
        var document = form.LicenseDocument is { } file
            ? new LicenseDocumentUpload(content!, file.FileName, file.ContentType, file.Length)
            : null;
        await sender.Send(new ResubmitLicenseCommand(form.LicenseType, form.LicenseNumber, form.IssueDate, form.ExpiryDate, document), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType<LawyerMeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerMeDto>> Me(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetLawyerMeQuery(), cancellationToken));

    [HttpPost("email/resend")]
    public async Task<IActionResult> ResendVerificationEmail(CancellationToken cancellationToken)
    {
        await sender.Send(new ResendLawyerVerificationEmailCommand(), cancellationToken);
        return NoContent();
    }

    /// <summary>The open registration-fee invoice (created on first call), priced from the admin setting.</summary>
    [HttpGet("registration-fee")]
    [ProducesResponseType<RegistrationFeeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationFeeDto>> RegistrationFee(CancellationToken cancellationToken)
        => Ok(await sender.Send(new OpenRegistrationFeeInvoiceCommand(), cancellationToken));

    public record PayRegistrationFeeBody(string? DiscountCode);

    [HttpPost("registration-fee/pay")]
    [ProducesResponseType<CheckoutResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutResultDto>> PayRegistrationFee(PayRegistrationFeeBody body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new PayRegistrationFeeCommand(body.DiscountCode), cancellationToken));

    [HttpGet("dashboard")]
    [ProducesResponseType<LawyerDashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerDashboardDto>> Dashboard(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetLawyerDashboardQuery(), cancellationToken));

    [HttpGet("profile")]
    [ProducesResponseType<LawyerProfileEditDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerProfileEditDto>> GetProfile(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMyProfileForEditQuery(), cancellationToken));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateLawyerProfileCommand(body.BioAr, body.BioEn, body.AcceptingNewRequests, body.SpecialtyIds, body.LanguageIds), cancellationToken);
        return NoContent();
    }

    [HttpPut("pricing")]
    public async Task<IActionResult> UpdatePricing(PricingBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateLawyerPricingCommand(body.WrittenPrice, body.Price15, body.Price30, body.Price45), cancellationToken);
        return NoContent();
    }

    [HttpPost("license/renew")]
    public async Task<IActionResult> RenewLicense(RenewLicenseBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RenewLicenseCommand(body.LicenseNumber, body.IssueDate, body.ExpiryDate), cancellationToken);
        return NoContent();
    }

    [HttpGet("earnings")]
    [ProducesResponseType<IReadOnlyList<PayoutSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PayoutSummaryDto>>> Earnings(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMyEarningsQuery(), cancellationToken));

    [HttpGet("reviews")]
    [ProducesResponseType<IReadOnlyList<LawyerReviewDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LawyerReviewDto>>> Reviews(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetLawyerReviewsQuery(), cancellationToken));
}
