using LawPortal.Application.Admin.DiscountCodes;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/discount-codes")]
public class AdminDiscountCodesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminDiscountCodeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminDiscountCodeDto>>> List([FromQuery] GetDiscountCodesQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminDiscountCodeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDiscountCodeDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetDiscountCodeQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(SaveDiscountCodeCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { Id = null }, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveDiscountCodeCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/redemptions")]
    [ProducesResponseType<IReadOnlyList<DiscountRedemptionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DiscountRedemptionDto>>> Redemptions(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetDiscountRedemptionsQuery(id), cancellationToken));
}
