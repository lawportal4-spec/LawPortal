using LawPortal.Application.Admin.LawyerDebts;
using LawPortal.Application.Lawyers.Onboarding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/settings")]
public class AdminSettingsController(ISender sender) : ControllerBase
{
    [HttpGet("registration-fee")]
    [ProducesResponseType<RegistrationFeeSettingDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationFeeSettingDto>> RegistrationFee(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRegistrationFeeSettingQuery(), cancellationToken));

    [HttpPut("registration-fee")]
    [ProducesResponseType<RegistrationFeeSettingDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationFeeSettingDto>> UpdateRegistrationFee(UpdateRegistrationFeeSettingCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpGet("refund-policy")]
    public async Task<ActionResult<RefundPolicyDto>> RefundPolicy(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRefundPolicyQuery(), cancellationToken));

    [HttpPut("refund-policy")]
    public async Task<ActionResult<RefundPolicyDto>> UpdateRefundPolicy(UpdateRefundPolicyCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));
}
