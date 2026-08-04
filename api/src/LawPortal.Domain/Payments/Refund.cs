using LawPortal.Domain.Common;

namespace LawPortal.Domain.Payments;

public class Refund : Entity<Guid>
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? GatewayRefundId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
