using LawPortal.Domain.Common;

namespace LawPortal.Domain.Billing;

/// <summary>The one-off fee an approved lawyer pays before the portal opens. A single row
/// (<see cref="SingletonId"/>), edited by admins; when the row doesn't exist yet the defaults apply.</summary>
public class RegistrationFeeSetting : Entity<int>
{
    public const int SingletonId = 1;
    public const decimal DefaultAmount = 500m;

    /// <summary>Before VAT — the lawyer pays this plus 15%.</summary>
    public decimal Amount { get; set; } = DefaultAmount;
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum RegistrationFeeInvoiceStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
}

/// <summary>Bill and, once paid, tax invoice for the registration fee — the platform's own
/// VAT-applicable supply, like <see cref="Subscriptions.SubscriptionInvoice"/>. Unlike subscription
/// prices, the fee is stated before VAT: <see cref="BaseAmount"/> 500 → <see cref="Total"/> 575.</summary>
public class LawyerRegistrationFeeInvoice : AggregateRoot<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public required string Number { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    /// <summary><see cref="BaseAmount"/> − <see cref="DiscountAmount"/>.</summary>
    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }

    public RegistrationFeeInvoiceStatus Status { get; set; } = RegistrationFeeInvoiceStatus.Pending;
    public DateTime? PaidAtUtc { get; set; }
    public string? FailureReason { get; set; }

    public string GatewayProvider { get; set; } = "";
    public string? GatewayPaymentId { get; set; }
    public string? QrPayloadBase64 { get; set; }
}
