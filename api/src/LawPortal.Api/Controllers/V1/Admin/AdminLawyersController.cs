using LawPortal.Application.Admin.Lawyers;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/lawyers")]
public class AdminLawyersController(ISender sender) : ControllerBase
{
    public record RejectBody(string Reason);
    public record RequestChangesBody(IReadOnlyList<LicenseCorrectionIssue> Issues, string? Note);

    [HttpGet]
    [ProducesResponseType<PagedResult<LawyerRegistrationSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LawyerRegistrationSummaryDto>>> List(
        [FromQuery] LicenseVerificationStatus? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ListLawyerRegistrationsQuery(status, search, page, pageSize), cancellationToken));

    [HttpGet("{lawyerProfileId:guid}")]
    [ProducesResponseType<LawyerRegistrationDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerRegistrationDetailDto>> Get(Guid lawyerProfileId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetLawyerRegistrationQuery(lawyerProfileId), cancellationToken));

    [HttpPost("{lawyerProfileId:guid}/request-changes")]
    public async Task<IActionResult> RequestChanges(Guid lawyerProfileId, RequestChangesBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RequestLawyerChangesCommand(lawyerProfileId, body.Issues, body.Note), cancellationToken);
        return NoContent();
    }

    [HttpPost("{lawyerProfileId:guid}/verify")]
    public async Task<IActionResult> Verify(Guid lawyerProfileId, CancellationToken cancellationToken)
    {
        await sender.Send(new VerifyLawyerCommand(lawyerProfileId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{lawyerProfileId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid lawyerProfileId, RejectBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RejectLawyerCommand(lawyerProfileId, body.Reason), cancellationToken);
        return NoContent();
    }
}
