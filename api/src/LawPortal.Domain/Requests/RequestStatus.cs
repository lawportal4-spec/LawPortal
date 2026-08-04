namespace LawPortal.Domain.Requests;

/// <summary>
/// P3 shipped and verified "Submitted" as the finalized, no-more-edits state — exactly what a
/// separate PendingPayment gate would have meant, so P4 adds <see cref="Paid"/> directly after
/// it rather than reworking already-shipped behavior for a distinction without a difference.
/// P6 adds <see cref="InProgress"/> (the lawyer accepted) and <see cref="Completed"/> (the
/// lawyer marked the work done — the real trigger for payout release, replacing P4's
/// admin-triggered stopgap) for <c>ConsultationRequest</c>s only; catalog requests have no
/// lawyer assigned at payment time (see <c>Payments.Payment.LawyerProfileId</c>'s docs) and stay
/// at <see cref="Paid"/> until a lawyer-assignment mechanism exists (P9).
/// <see cref="Refunded"/> is a terminal exit only for a FULL refund; a partial refund is a
/// <see cref="Payments.Refund"/> record and does not change the request's status.
/// </summary>
public enum RequestStatus
{
    Draft = 0,
    Submitted = 20,
    /// <summary><see cref="BiddingRequest"/> only — the client accepted an offer, but hasn't
    /// paid yet. For consultations/catalog requests, <see cref="Submitted"/> goes straight to
    /// <see cref="Paid"/>; bidding needs this extra step because there's no known price (and
    /// so no known lawyer to pay) until an offer is actually accepted.</summary>
    Awarded = 25,
    Paid = 30,
    InProgress = 40,
    Completed = 50,
    Cancelled = 90,
    Refunded = 95,
}

public enum ConsultationType
{
    Instant = 1,
    Written = 2,
    Scheduled = 3,
}
