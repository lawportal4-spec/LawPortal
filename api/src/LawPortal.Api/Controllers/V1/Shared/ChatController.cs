using LawPortal.Application.Chat.Commands;
using LawPortal.Application.Chat.Dtos;
using LawPortal.Application.Chat.Queries;
using LawPortal.Domain.Chat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Shared;

/// <summary>Symmetric between clients and lawyers — both sides of a thread hit the same
/// endpoints, since chat itself doesn't distinguish the two roles.</summary>
[ApiController]
[Authorize]
[Route("api/v1/chat")]
public class ChatController(ISender sender) : ControllerBase
{
    public record SendMessageBody(string Body);
    public record ReportBody(ReportReason Reason, string? Details);

    [HttpGet("threads")]
    [ProducesResponseType<IReadOnlyList<ThreadSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ThreadSummaryDto>>> MyThreads(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMyThreadsQuery(), cancellationToken));

    [HttpGet("threads/{id:guid}")]
    [ProducesResponseType<ThreadDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ThreadDetailDto>> ThreadDetail(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetThreadDetailQuery(id), cancellationToken));

    [HttpGet("threads/{id:guid}/messages")]
    [ProducesResponseType<IReadOnlyList<MessageDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> Messages(
        Guid id, [FromQuery] DateTime? after, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetThreadMessagesQuery(id, after, take), cancellationToken));

    [HttpPost("threads/{id:guid}/messages")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageDto>> Send(Guid id, SendMessageBody body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new SendMessageCommand(id, body.Body), cancellationToken));

    [HttpPost("threads/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkThreadReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("threads/{id:guid}/report")]
    public async Task<IActionResult> Report(Guid id, ReportBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new ReportUserCommand(id, body.Reason, body.Details), cancellationToken);
        return NoContent();
    }

    [HttpPost("threads/{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new BlockUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("threads/{id:guid}/block")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new UnblockUserCommand(id), cancellationToken);
        return NoContent();
    }
}
