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

    /// <summary>PDPL/data-portability: a machine-readable copy of the caller's own data.</summary>
    [HttpGet("export")]
    public async Task<ActionResult<AccountExportDto>> Export(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExportMyDataQuery(), cancellationToken);
        return Ok(result);
    }
}
