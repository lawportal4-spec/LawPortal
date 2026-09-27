using LawPortal.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Payments;

/// <summary>
/// The landing spot for <c>success_url</c>/<c>back_url</c> on a hosted gateway page. The gateway
/// only knows the id it was given at checkout, not which of our three apps the payer came from, so
/// it redirects here and we work that out — see <see cref="GetPaymentReturnTargetQuery"/> for why
/// this is unauthenticated and why it is safe.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
public class PaymentsReturnController(ISender sender) : ControllerBase
{
    [HttpGet("{paymentId:guid}/return")]
    public async Task<IActionResult> Return(Guid paymentId, CancellationToken cancellationToken)
        => Redirect(await sender.Send(new GetPaymentReturnTargetQuery(paymentId), cancellationToken));
}
