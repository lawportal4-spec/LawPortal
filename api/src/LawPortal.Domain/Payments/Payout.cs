using LawPortal.Domain.Common;
using LawPortal.Domain.Identity;

namespace LawPortal.Domain.Payments;

/// <summary>
/// Release of escrowed funds to the lawyer who fulfilled the request. Only created for payments
/// with a known <see cref="Payment.LawyerProfileId"/> at posting time. The trigger for release
/// is a lawyer marking work complete (P6) — until that exists, release is admin-triggered as a
/// deliberate stopgap so the escrow-hold-then-release ledger path can be built and verified now.
/// </summary>
public class Payout : Entity<Guid>
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Held;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAtUtc { get; set; }
    public string? Notes { get; set; }
}
