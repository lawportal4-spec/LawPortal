using LawPortal.Application.Admin.Directory;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

/// <summary>Clients, requests, a lawyer's finances, account suspension, internal notes and the global search.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminDirectoryController(ISender sender) : ControllerBase
{
    public record SuspendBody(bool Suspended, string Reason);
    public record NoteBody(string Body);

    [HttpGet("clients")]
    public async Task<ActionResult<AdminClientListDto>> Clients([FromQuery] GetAdminClientsQuery query, CancellationToken ct) => Ok(await sender.Send(query, ct));

    [HttpGet("clients/{id:guid}")]
    public async Task<ActionResult<AdminClientDetailDto>> Client(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetAdminClientQuery(id), ct));

    [HttpGet("requests")]
    public async Task<ActionResult<AdminRequestListDto>> Requests([FromQuery] GetAdminRequestsQuery query, CancellationToken ct) => Ok(await sender.Send(query, ct));

    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<AdminRequestDetailDto>> Request(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetAdminRequestQuery(id), ct));

    /// <summary>Audit-logged on every call.</summary>
    [HttpGet("requests/{id:guid}/chat")]
    public async Task<ActionResult<IReadOnlyList<AdminChatMessageDto>>> Chat(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetAdminRequestChatQuery(id), ct));

    [HttpGet("lawyers/{id:guid}/finance")]
    public async Task<ActionResult<AdminLawyerFinanceDto>> LawyerFinance(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetAdminLawyerFinanceQuery(id), ct));

    [HttpPost("accounts/{userId:guid}/suspension")]
    public async Task<IActionResult> Suspend(Guid userId, SuspendBody body, CancellationToken ct)
    {
        await sender.Send(new SetAccountSuspendedCommand(userId, body.Suspended, body.Reason), ct);
        return NoContent();
    }

    [HttpGet("notes/{entityType}/{entityId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AdminNoteDto>>> Notes(string entityType, Guid entityId, CancellationToken ct) =>
        Ok(await sender.Send(new GetAdminNotesQuery(entityType, entityId), ct));

    [HttpPost("notes/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> AddNote(string entityType, Guid entityId, NoteBody body, CancellationToken ct)
    {
        await sender.Send(new AddAdminNoteCommand(entityType, entityId, body.Body), ct);
        return NoContent();
    }

    [HttpGet("page-stats/{page}")]
    public async Task<ActionResult<IReadOnlyList<PageStatDto>>> PageStats(string page, CancellationToken ct) => Ok(await sender.Send(new GetAdminPageStatsQuery(page), ct));

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<AdminSearchHitDto>>> Search([FromQuery] string q, CancellationToken ct) => Ok(await sender.Send(new AdminSearchQuery(q ?? ""), ct));
}
