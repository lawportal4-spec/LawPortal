using LawPortal.Application.Admin.Users.Commands;
using LawPortal.Application.Admin.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/users")]
public class AdminUsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminUserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> List(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAdminUsersQuery(), cancellationToken));

    [HttpPost("invite")]
    public async Task<ActionResult<Guid>> Invite(InviteAdminCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));
}
