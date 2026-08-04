using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Lawyers.Queries;
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
