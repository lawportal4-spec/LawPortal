using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Payments.Queries;

/// <summary>
/// Resolves where to send a payer's browser once it comes back from the gateway's hosted page.
///
/// Deliberately does not require an authenticated caller: the customer returns through a redirect
/// the gateway controls, so nothing guarantees our session survives the round trip. That is safe
/// here because this only ever maps an id to a URL — it reads no amounts, exposes no detail and
/// changes no state, and the page it lands on enforces its own auth. Settlement stays entirely
/// webhook-driven; this is navigation, not confirmation.
///
/// The id is whatever was passed to <c>IPaymentGateway.CreatePaymentAsync</c>: a Payment id for
/// marketplace checkout and wallet top-ups, a SubscriptionInvoice id for lawyer subscriptions
/// (see <c>PaySubscriptionInvoiceCommand</c>) — the same two-step dispatch the webhook handler does.
/// </summary>
public record GetPaymentReturnTargetQuery(Guid PaymentId) : IRequest<string>;

public class GetPaymentReturnTargetHandler(ILawPortalDbContext db, IConfiguration configuration)
    : IRequestHandler<GetPaymentReturnTargetQuery, string>
{
    public async Task<string> Handle(GetPaymentReturnTargetQuery request, CancellationToken cancellationToken)
    {
        var clientBaseUrl = (configuration["Payments:ClientBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

        // ?payment=returned tells the landing page it is mid-settlement, so it polls for the
        // webhook rather than offering to start checkout over again.
        const string returned = "?payment=returned";

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);
        if (payment is not null)
        {
            // A null ServiceRequestId is a wallet top-up — see Payment.ServiceRequestId's docs.
            return payment.ServiceRequestId is { } requestId
                ? $"{clientBaseUrl}/orders/{requestId}{returned}"
                : $"{clientBaseUrl}/wallet{returned}";
        }

        var isSubscriptionInvoice = await db.SubscriptionInvoices
            .AnyAsync(i => i.Id == request.PaymentId, cancellationToken);
        if (isSubscriptionInvoice)
        {
            var lawyerBaseUrl = (configuration["Payments:LawyerBaseUrl"] ?? "http://localhost:5174").TrimEnd('/');
            return $"{lawyerBaseUrl}/subscription{returned}";
        }

        // An unknown id means a stale or hand-edited return URL. Land them somewhere sensible
        // rather than 404-ing a browser that has just finished paying.
        return $"{clientBaseUrl}/orders";
    }
}
