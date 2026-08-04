using LawPortal.Application.Notifications;
using LawPortal.Application.Notifications.Commands;
using LawPortal.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Shared;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> MyNotifications(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMyNotificationsQuery(), cancellationToken));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }
}
