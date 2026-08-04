using LawPortal.Application.Admin.Subscriptions.Commands;
using LawPortal.Application.Admin.Subscriptions.Dtos;
using LawPortal.Application.Admin.Subscriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/subscription-plans")]
public class AdminSubscriptionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminSubscriptionPlanDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminSubscriptionPlanDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAdminSubscriptionPlansQuery(), cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest();
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
