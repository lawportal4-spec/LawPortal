using LawPortal.Domain.Common;

namespace LawPortal.Domain.Payments;

/// <summary>Fixed list shown to the admin; labels live in i18n (<c>refund.reasons.*</c>).
/// <see cref="Other"/> requires <see cref="Refund.Details"/>.</summary>
public enum RefundReason
{
    ServiceNotDelivered = 1,
    LawyerNoResponse = 2,
    LawyerDeclined = 3,
    ClientCancelled = 4,
    DuplicatePayment = 5,
    TechnicalIssue = 6,
    QualityComplaint = 7,
    Other = 99,
}

/// <summary>Who covers the lawyer's share of a refund made after their payout was released.</summary>
public enum RefundBearer
{
    /// <summary>Recorded as a debt the lawyer owes the platform.</summary>
    Lawyer = 1,
    /// <summary>The platform absorbs it (e.g. the platform's own mistake).</summary>
    Platform = 2,
}

public class Refund : Entity<Guid>
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public decimal Amount { get; set; }
    public RefundReason Reason { get; set; }
    /// <summary>Free-text note; required when <see cref="Reason"/> is Other.</summary>
    public string? Details { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    /// <summary>Set when the lawyer's payout had already been released: who covers their share.</summary>
    public RefundBearer? LawyerShareBearer { get; set; }
    public string? GatewayRefundId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
