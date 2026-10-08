using LawPortal.Application.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Shared;

[ApiController]
[Authorize]
[Route("api/v1/account")]
public class AccountController(ISender sender) : ControllerBase
{
    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAccountCommand(), cancellationToken);
        return NoContent();
    }

    /// <summary>What deleting would forfeit or leave owing — shown before the person confirms.</summary>
    [HttpGet("deletion-impact")]
    public async Task<ActionResult<DeletionImpactDto>> DeletionImpact(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetDeletionImpactQuery(), cancellationToken));

    [HttpGet("profile")]
    public async Task<ActionResult<MyProfileDto>> GetProfile(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyProfileQuery(), cancellationToken));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangeMyPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>PDPL/data-portability: a machine-readable copy of the caller's own data.</summary>
    [HttpGet("export")]
    public async Task<ActionResult<AccountExportDto>> Export(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExportMyDataQuery(), cancellationToken);
        return Ok(result);
    }
}
