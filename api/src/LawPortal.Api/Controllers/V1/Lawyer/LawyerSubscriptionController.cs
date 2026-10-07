using LawPortal.Application.Payments.Dtos;
using LawPortal.Application.Subscriptions.Commands;
using LawPortal.Application.Subscriptions.Dtos;
using LawPortal.Application.Subscriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LawPortal.Api.Controllers.V1.Lawyer;

[ApiController]
[Authorize(Roles = "Lawyer")]
[Route("api/v1/lawyer/subscription")]
public class LawyerSubscriptionController(ISender sender) : ControllerBase
{
    public record SubscribeBody(int PlanId);

    [HttpGet("plans")]
    [ProducesResponseType<IReadOnlyList<SubscriptionPlanDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> Plans(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSubscriptionPlansQuery(), cancellationToken));

    [HttpGet]
    [ProducesResponseType<MySubscriptionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MySubscriptionDto>> Mine(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMySubscriptionQuery(), cancellationToken));

    [HttpGet("invoices")]
    [ProducesResponseType<IReadOnlyList<SubscriptionInvoiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionInvoiceDto>>> Invoices(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMySubscriptionInvoicesQuery(), cancellationToken));

    [HttpPost("subscribe")]
    [ProducesResponseType<SubscriptionInvoiceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionInvoiceDto>> Subscribe(SubscribeBody body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new SubscribeToPlanCommand(body.PlanId), cancellationToken));

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        await sender.Send(new CancelSubscriptionCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPost("invoices/{id:guid}/pay")]
    [ProducesResponseType<CheckoutResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutResultDto>> PayInvoice(
        Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PayInvoiceBody? body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new PaySubscriptionInvoiceCommand(id, body?.DiscountCode), cancellationToken));

    public record PayInvoiceBody(string? DiscountCode);
}
