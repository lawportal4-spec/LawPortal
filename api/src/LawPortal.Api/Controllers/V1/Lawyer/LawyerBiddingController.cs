using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Lawyers.Queries;
using LawPortal.Application.Offers.Commands;
using LawPortal.Application.Offers.Dtos;
using LawPortal.Application.Offers.Queries;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Lawyer;

/// <summary>The lawyer's side of bidding: the feed of invited requests, one request's detail
/// (attachments locked until awarded), and offer submission/withdrawal. Awarded requests move to
/// <see cref="LawyerRequestsController"/>'s shared <c>complete</c> action once paid.</summary>
[ApiController]
[Authorize(Roles = "Lawyer")]
[Route("api/v1/lawyer/bidding")]
public class LawyerBiddingController(ISender sender) : ControllerBase
{
    public record SubmitOfferBody(decimal Amount, string? Message);

    [HttpGet("feed")]
    [ProducesResponseType<PagedResult<BiddingFeedItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BiddingFeedItemDto>>> Feed(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetBiddingFeedQuery(page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BiddingRequestDetailForLawyerDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BiddingRequestDetailForLawyerDto>> Detail(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetBiddingRequestDetailForLawyerQuery(id), cancellationToken));

    [HttpPost("{id:guid}/offer")]
    public async Task<ActionResult<Guid>> SubmitOffer(Guid id, SubmitOfferBody body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new SubmitOfferCommand(id, body.Amount, body.Message), cancellationToken));

    [HttpPost("offers/{offerId:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid offerId, CancellationToken cancellationToken)
    {
        await sender.Send(new WithdrawOfferCommand(offerId), cancellationToken);
        return NoContent();
    }

    [HttpGet("awarded")]
    [ProducesResponseType<PagedResult<LawyerRequestSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LawyerRequestSummaryDto>>> Awarded(
        [FromQuery] RequestStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetAwardedBiddingRequestsQuery(status, page, pageSize), cancellationToken));
}
