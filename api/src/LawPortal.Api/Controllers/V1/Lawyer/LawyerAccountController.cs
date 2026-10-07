using LawPortal.Application.Lawyers.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Lawyer;

/// <summary>The lawyer's personal details page ("البيانات الشخصية").</summary>
[ApiController]
[Authorize(Roles = "Lawyer")]
[Route("api/v1/lawyer/account")]
public class LawyerAccountController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<LawyerAccountDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerAccountDto>> Get(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetLawyerAccountQuery(), cancellationToken));

    public record PhotoResult(string? PhotoUrl);

    [HttpPost("photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<PhotoResult>> UploadPhoto(IFormFile photo, CancellationToken cancellationToken)
    {
        await using var content = photo.OpenReadStream();
        var url = await sender.Send(new SetLawyerPhotoCommand(new PhotoUpload(content, photo.FileName, photo.ContentType, photo.Length)), cancellationToken);
        return Ok(new PhotoResult(url));
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> RemovePhoto(CancellationToken cancellationToken)
    {
        await sender.Send(new SetLawyerPhotoCommand(null), cancellationToken);
        return NoContent();
    }

    public record PhoneBody(string PhoneE164);
    public record PhoneCodeBody(string PhoneE164, string Code);

    [HttpPost("phone/request")]
    public async Task<IActionResult> RequestPhoneChange(PhoneBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RequestLawyerPhoneChangeCommand(body.PhoneE164), cancellationToken);
        return NoContent();
    }

    [HttpPost("phone/confirm")]
    public async Task<IActionResult> ConfirmPhoneChange(PhoneCodeBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmLawyerPhoneChangeCommand(body.PhoneE164, body.Code), cancellationToken);
        return NoContent();
    }

    public record EmailBody(string Email);

    [HttpPost("email/request")]
    public async Task<IActionResult> RequestEmailChange(EmailBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RequestLawyerEmailChangeCommand(body.Email), cancellationToken);
        return NoContent();
    }

    [HttpPut("location")]
    public async Task<IActionResult> UpdateLocation(UpdateLawyerLocationCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("contact-numbers")]
    public async Task<IActionResult> SaveContactNumbers(SaveLawyerContactNumbersCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
