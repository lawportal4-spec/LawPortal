using LawPortal.Application.Admin.LawyerDebts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/lawyer-debts")]
public class AdminLawyerDebtsController(ISender sender) : ControllerBase
{
    public record RepaymentBody(decimal Amount, string Reference, string? Note);

    [HttpGet]
    public async Task<ActionResult<LawyerDebtListDto>> List([FromQuery] string? collection, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetLawyerDebtsQuery(collection), cancellationToken));

    [HttpGet("{lawyerProfileId:guid}")]
    public async Task<ActionResult<LawyerDebtDetailDto>> Get(Guid lawyerProfileId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetLawyerDebtQuery(lawyerProfileId), cancellationToken));

    [HttpPost("{lawyerProfileId:guid}/repayments")]
    public async Task<IActionResult> RecordRepayment(Guid lawyerProfileId, RepaymentBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new RecordLawyerDebtRepaymentCommand(lawyerProfileId, body.Amount, body.Reference, body.Note), cancellationToken);
        return NoContent();
    }

    [HttpPost("{lawyerProfileId:guid}/reminders")]
    public async Task<IActionResult> SendReminder(Guid lawyerProfileId, CancellationToken cancellationToken)
    {
        await sender.Send(new SendLawyerDebtReminderCommand(lawyerProfileId), cancellationToken);
        return NoContent();
    }
}
