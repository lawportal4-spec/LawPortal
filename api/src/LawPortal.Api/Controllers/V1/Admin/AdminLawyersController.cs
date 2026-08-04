using LawPortal.Application.Admin.Lawyers;
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

    [HttpGet("pending")]
    [ProducesResponseType<IReadOnlyList<PendingLawyerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PendingLawyerDto>>> ListPending(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListPendingLawyersQuery(), cancellationToken);
        return Ok(result);
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
