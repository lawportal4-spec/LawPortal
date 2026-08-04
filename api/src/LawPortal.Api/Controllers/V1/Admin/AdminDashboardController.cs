using LawPortal.Application.Admin.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/dashboard")]
public class AdminDashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AdminDashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardDto>> Get(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAdminDashboardQuery(), cancellationToken));
}
