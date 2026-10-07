namespace LawPortal.Domain.Payments;

/// <summary>The bank-side record of a card payment as the gateway reports it. Only the masked card
/// number (first 6 / last 4) is kept; never the full number or the cardholder's name.</summary>
public class GatewayTransaction
{
    /// <summary>The gateway's own payment id (Moyasar: <c>pay_…</c>; the invoice id stays on <see cref="Payment.GatewayPaymentId"/>).</summary>
    public string? TransactionId { get; set; }
    /// <summary>creditcard, applepay, stcpay…</summary>
    public string? SourceType { get; set; }
    /// <summary>mada, visa, master, amex…</summary>
    public string? CardBrand { get; set; }
    public string? CardMasked { get; set; }
    /// <summary>The bank's retrieval reference number (RRN).</summary>
    public string? ReferenceNumber { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? ResponseCode { get; set; }
    public string? Message { get; set; }
    /// <summary>The gateway's fee in SAR, when it reports one.</summary>
    public decimal? Fee { get; set; }
    public DateTime FetchedAtUtc { get; set; }
}
