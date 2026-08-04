using LawPortal.Domain.Common;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Requests;

namespace LawPortal.Domain.Payments;

/// <summary>
/// One payment attempt, via card/gateway or wallet balance, for either a request checkout or a
/// wallet top-up — <see cref="ServiceRequestId"/> is null for the latter. VAT and commission are
/// snapshotted here at posting time (not re-derived from current policy later), so a refund
/// months after a commission-rate change still reverses exactly what was actually charged.
/// </summary>
public class Payment : AggregateRoot<Guid>
{
    public required string Number { get; set; }
    public PaymentPurpose Purpose { get; set; }

    public Guid? ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }

    public Guid ClientId { get; set; }
    public ClientProfile? Client { get; set; }

    /// <summary>Snapshotted from the request's assigned lawyer at checkout time. Null for
    /// catalog requests (notarization/trademark), which have no lawyer assigned yet at payment
    /// time — see <see cref="Requests.CatalogRequest"/>; escrow for those sits unattributed
    /// until a lawyer-assignment mechanism exists (P6/P9).</summary>
    public Guid? LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public decimal Total { get; set; }
    public decimal VatAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetToLawyerAmount { get; set; }
    public bool IsVatApplicable { get; set; }
    public string CurrencyCode { get; set; } = "SAR";

    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
    public string MethodDescription { get; set; } = "Card";

    public string GatewayProvider { get; set; } = "";
    public string? GatewayPaymentId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public ICollection<Refund> Refunds { get; set; } = [];
    public Payout? Payout { get; set; }
}
