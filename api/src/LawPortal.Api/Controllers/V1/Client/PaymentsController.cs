using LawPortal.Application.Payments.Commands;
using LawPortal.Application.Payments.Dtos;
using LawPortal.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Client;

[ApiController]
[Authorize]
[Route("api/v1/client/payments")]
public class PaymentsController(ISender sender) : ControllerBase
{
    public record CheckoutBody(Guid RequestId, string PaymentMethod);

    [HttpPost("checkout")]
    [ProducesResponseType<CheckoutResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutResultDto>> Checkout(CheckoutBody body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new InitiateCheckoutCommand(body.RequestId, body.PaymentMethod), cancellationToken));

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType<PaymentSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSummaryDto>> Status(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPaymentStatusQuery(id), cancellationToken));
}
