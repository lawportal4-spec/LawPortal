using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Payments;

namespace LawPortal.Application.Payments;

/// <summary>Fetches the bank-side details of a card payment from the gateway that processed it.</summary>
public static class PaymentTransactionDetails
{
    public static bool NeedsFetch(Payment payment, IPaymentGateway gateway) =>
        payment.Transaction is null
        && payment.MethodDescription != "Wallet"
        && payment.GatewayPaymentId is not null
        && payment.GatewayProvider == gateway.Name
        // Only money that actually moved has bank details; a failed payment shows its failure reason instead.
        && payment.Status is PaymentStatus.Paid or PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded;

    /// <summary>Never fails the caller: a confirmed payment must not be rolled back because the details
    /// lookup timed out. A miss stays null and is fetched again the next time an admin opens it.</summary>
    public static async Task<GatewayTransaction?> TryFetchAsync(IPaymentGateway gateway, Payment payment, CancellationToken cancellationToken)
    {
        if (payment.GatewayPaymentId is null || payment.GatewayProvider != gateway.Name) return null;
        try
        {
            return await gateway.GetTransactionAsync(payment.GatewayPaymentId, cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            return null;
        }
    }
}
