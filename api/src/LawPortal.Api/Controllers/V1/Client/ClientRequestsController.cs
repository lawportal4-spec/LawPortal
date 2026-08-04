using LawPortal.Application.Payments.Dtos;
using LawPortal.Application.Payments.Queries;
using LawPortal.Application.Requests.Commands;
using LawPortal.Application.Requests.Dtos;
using LawPortal.Application.Requests.Queries;
using LawPortal.Application.Reviews.Commands;
using LawPortal.Domain.Requests;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Client;

[ApiController]
[Authorize]
[Route("api/v1/client/requests")]
public class ClientRequestsController(ISender sender) : ControllerBase
{
    public record CancelBody(string? Reason);

    [HttpPost("consultations")]
    public async Task<ActionResult<Guid>> CreateConsultationDraft(CreateConsultationDraftCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPut("consultations/{id:guid}/details")]
    public async Task<IActionResult> UpdateConsultationDetails(Guid id, UpdateConsultationDetailsBody body, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateConsultationDetailsCommand(id, body.Title, body.Description, body.ScheduledStartUtc, body.VoiceNoteStorageKey, body.VoiceNoteDurationSeconds),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("catalog")]
    public async Task<ActionResult<Guid>> CreateCatalogDraft(CreateCatalogDraftCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitRequestCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancelBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelRequestCommand(id, body.Reason), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<RequestSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RequestSummaryDto>>> List(
        [FromQuery] RequestStatus? status,
        [FromQuery] string? kind,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetMyRequestsQuery(status, kind, from, to, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestDetailDto>> Detail(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRequestDetailQuery(id), cancellationToken));

    [HttpGet("{id:guid}/invoice")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceDto>> Invoice(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetInvoiceQuery(id), cancellationToken));

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> SubmitReview(Guid id, ReviewBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitReviewCommand(id, body.Rating, body.Comment), cancellationToken);
        return NoContent();
    }
}

public record UpdateConsultationDetailsBody(
    string Title,
    string Description,
    DateTime? ScheduledStartUtc,
    string? VoiceNoteStorageKey,
    int? VoiceNoteDurationSeconds);

public record ReviewBody(int Rating, string? Comment);
