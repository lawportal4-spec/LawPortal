using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Admin.Finance.Queries;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Payments.Commands;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/payments")]
public class AdminPaymentsController(ISender sender) : ControllerBase
{
    public record RefundBody(decimal Amount, string Reason);

    [HttpGet]
    [ProducesResponseType<PagedResult<AdminPaymentSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminPaymentSummaryDto>>> List(
        [FromQuery] PaymentStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetAdminPaymentsQuery(status, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminPaymentDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminPaymentDetailDto>> Detail(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAdminPaymentDetailQuery(id), cancellationToken));

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, RefundBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RefundPaymentCommand(id, body.Amount, body.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("payouts/{payoutId:guid}/release")]
    public async Task<IActionResult> ReleasePayout(Guid payoutId, CancellationToken cancellationToken)
    {
        await sender.Send(new ReleasePayoutCommand(payoutId), cancellationToken);
        return NoContent();
    }
}
