using LawPortal.Application.Admin.Finance.Commands;
using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Admin.Finance.Queries;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/finance")]
public class AdminFinanceController(ISender sender) : ControllerBase
{
    public record CreateCommissionPolicyBody(string? ServiceCategorySlug, decimal Percentage, DateTime? EffectiveFromUtc);

    [HttpGet("ledger/summary")]
    [ProducesResponseType<LedgerSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LedgerSummaryDto>> LedgerSummary([FromQuery] GetLedgerSummaryQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [HttpGet("ledger/journal")]
    [ProducesResponseType<PagedResult<JournalEntryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<JournalEntryDto>>> LedgerJournal([FromQuery] GetLedgerJournalQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [HttpGet("ledger/entries")]
    [ProducesResponseType<PagedResult<LedgerEntryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LedgerEntryDto>>> LedgerEntries(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetLedgerEntriesQuery(page, pageSize), cancellationToken));

    [HttpGet("commission-policies")]
    [ProducesResponseType<IReadOnlyList<CommissionPolicyDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommissionPolicyDto>>> CommissionPolicies(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCommissionPoliciesQuery(), cancellationToken));

    [HttpPost("commission-policies")]
    public async Task<ActionResult<int>> CreateCommissionPolicy(CreateCommissionPolicyBody body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new CreateCommissionPolicyCommand(body.ServiceCategorySlug, body.Percentage, body.EffectiveFromUtc), cancellationToken));
}
