using LawPortal.Application.Calls.Commands;
using LawPortal.Application.Calls.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Shared;

/// <summary>Symmetric between clients and lawyers, like chat — whoever calls this for a
/// request they're a participant in gets a token for the same LiveKit room.</summary>
[ApiController]
[Authorize]
[Route("api/v1/calls")]
public class CallsController(ISender sender) : ControllerBase
{
    [HttpPost("{requestId:guid}/token")]
    [ProducesResponseType<CallTokenDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CallTokenDto>> IssueToken(Guid requestId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new IssueCallTokenCommand(requestId), cancellationToken));
}
