using LawPortal.Application.Attachments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Client;

[ApiController]
[Authorize]
[Route("api/v1")]
public class AttachmentsController(ISender sender) : ControllerBase
{
    [HttpPost("attachments/presign")]
    [ProducesResponseType<PresignedUploadDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PresignedUploadDto>> Presign(CreateUploadUrlCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPost("client/requests/{id:guid}/attachments/confirm")]
    [ProducesResponseType<ConfirmedAttachmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfirmedAttachmentDto>> Confirm(
        Guid id, ConfirmAttachmentBody body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ConfirmAttachmentCommand(id, body.StorageKey, body.FileName, body.ContentType),
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("client/requests/{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> Remove(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveAttachmentCommand(id, attachmentId), cancellationToken);
        return NoContent();
    }
}

public record ConfirmAttachmentBody(string StorageKey, string FileName, string ContentType);
