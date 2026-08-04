using LawPortal.Domain.Common;

namespace LawPortal.Domain.Subscriptions;

public enum SubscriptionInvoiceStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
}

/// <summary>
/// Both the bill and, once paid, the invoice — unlike a marketplace checkout (which splits
/// <see cref="Payments.Payment"/> from <see cref="Billing.Invoice"/> because a lawyer's VAT
/// registration decides invoice fields independently of payment mechanics), a subscription fee
/// is always the platform's own VAT-applicable supply, sold under the platform's own VAT number,
/// so one record covers both. Goes through the same <c>IPaymentGateway</c> Card flow and the
/// same webhook (<c>HandleGatewayWebhookCommand</c>, extended to check subscription invoices
/// when no matching <see cref="Payments.Payment"/> exists) as every other payment in this app.
/// </summary>
public class SubscriptionInvoice : AggregateRoot<Guid>
{
    public Guid LawyerSubscriptionId { get; set; }
    public LawyerSubscription? LawyerSubscription { get; set; }

    public required string Number { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }

    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }

    public SubscriptionInvoiceStatus Status { get; set; } = SubscriptionInvoiceStatus.Pending;
    public DateTime DueAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? FailureReason { get; set; }

    public string GatewayProvider { get; set; } = "";
    public string? GatewayPaymentId { get; set; }
    public string? QrPayloadBase64 { get; set; }
}
