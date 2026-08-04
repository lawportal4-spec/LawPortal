using LawPortal.Application.Offers.Commands;
using LawPortal.Application.Offers.Dtos;
using LawPortal.Application.Offers.Queries;
using LawPortal.Application.Requests.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Client;

/// <summary>The client's side of the bidding wizard (draft + details — submit/cancel/list/detail
/// are the shared endpoints already on <see cref="ClientRequestsController"/>) plus the offer
/// inbox and negotiation actions.</summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public class ClientBiddingController(ISender sender) : ControllerBase
{
    public record CounterBody(decimal Amount, string? Message);

    [HttpPost("client/bidding")]
    public async Task<ActionResult<Guid>> CreateDraft(CreateBiddingDraftCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPut("client/bidding/{id:guid}/details")]
    public async Task<IActionResult> UpdateDetails(Guid id, UpdateBiddingDetailsBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateBiddingDetailsCommand(id, body.Title, body.Description), cancellationToken);
        return NoContent();
    }

    [HttpGet("client/bidding/{id:guid}/offers")]
    [ProducesResponseType<IReadOnlyList<OfferSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OfferSummaryDto>>> Inbox(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetOfferInboxQuery(id), cancellationToken));

    [HttpPost("client/offers/{offerId:guid}/counter")]
    public async Task<IActionResult> Counter(Guid offerId, CounterBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new CounterOfferCommand(offerId, body.Amount, body.Message), cancellationToken);
        return NoContent();
    }

    [HttpPost("client/offers/{offerId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid offerId, CancellationToken cancellationToken)
    {
        await sender.Send(new AcceptOfferCommand(offerId), cancellationToken);
        return NoContent();
    }

    [HttpPost("client/offers/{offerId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid offerId, CancellationToken cancellationToken)
    {
        await sender.Send(new RejectOfferCommand(offerId), cancellationToken);
        return NoContent();
    }
}

public record UpdateBiddingDetailsBody(string Title, string Description);
