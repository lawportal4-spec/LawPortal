using LawPortal.Application.Payments.Commands;
using LawPortal.Application.Payments.Dtos;
using LawPortal.Application.Wallet.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Client;

[ApiController]
[Authorize]
[Route("api/v1/client/wallet")]
public class WalletController(ISender sender) : ControllerBase
{
    public record TopUpBody(decimal Amount);

    [HttpGet]
    [ProducesResponseType<WalletDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletDto>> Get(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetWalletQuery(), cancellationToken));

    [HttpGet("transactions")]
    [ProducesResponseType<IReadOnlyList<WalletTransactionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WalletTransactionDto>>> Transactions(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetWalletTransactionsQuery(), cancellationToken));

    [HttpPost("topup")]
    [ProducesResponseType<CheckoutResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutResultDto>> TopUp(TopUpBody body, CancellationToken cancellationToken)
        => Ok(await sender.Send(new InitiateWalletTopUpCommand(body.Amount), cancellationToken));
}
