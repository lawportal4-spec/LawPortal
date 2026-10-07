using LawPortal.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Shared;

[ApiController]
[Authorize]
[Route("api/v1/discount-codes")]
public class CheckoutPreviewController(ISender sender) : ControllerBase
{
    /// <summary>The amounts a checkout will charge, with an optional discount code applied.</summary>
    [HttpPost("preview")]
    [ProducesResponseType<CheckoutPreviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutPreviewDto>> Preview(PreviewCheckoutQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));
}
