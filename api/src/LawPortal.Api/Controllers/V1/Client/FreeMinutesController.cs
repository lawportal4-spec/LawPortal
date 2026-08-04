using LawPortal.Application.FreeMinutes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Client;

[ApiController]
[Authorize]
[Route("api/v1/client/free-minutes")]
public class FreeMinutesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FreeMinutesBalanceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FreeMinutesBalanceDto>> Balance(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetFreeMinutesBalanceQuery(), cancellationToken));
}
