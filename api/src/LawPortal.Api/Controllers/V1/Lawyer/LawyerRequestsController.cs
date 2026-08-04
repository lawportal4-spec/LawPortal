using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Lawyers.Queries;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Lawyer;

[ApiController]
[Authorize(Roles = "Lawyer")]
[Route("api/v1/lawyer/requests")]
public class LawyerRequestsController(ISender sender) : ControllerBase
{
    public record DeclineBody(string Reason);

    [HttpGet]
    [ProducesResponseType<PagedResult<LawyerRequestSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LawyerRequestSummaryDto>>> List(
        [FromQuery] RequestStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetIncomingRequestsQuery(status, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<LawyerRequestDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerRequestDetailDto>> Detail(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetIncomingRequestDetailQuery(id), cancellationToken));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new AcceptRequestCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, DeclineBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new DeclineRequestCommand(id, body.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CompleteRequestCommand(id), cancellationToken);
        return NoContent();
    }
}
