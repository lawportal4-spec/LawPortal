using LawPortal.Domain.Common;

namespace LawPortal.Domain.Requests;

public enum OfferStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Withdrawn = 4,
    Expired = 5,
}

public enum OfferProposedBy
{
    Lawyer = 1,
    Client = 2,
}

/// <summary>
/// "Offers are negotiable, not sealed bids" (the plan's own reading of the docs) — one `Offer`
/// per lawyer per request, but its price moves over time via <see cref="OfferRevision"/> rows as
/// either side counters. <see cref="Status"/> tracks the negotiation's outcome, not its current
/// price — read the latest revision for that.
/// </summary>
public class Offer : Entity<Guid>
{
    public Guid ServiceRequestId { get; set; }
    public Guid LawyerProfileId { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Pending;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<OfferRevision> Revisions { get; set; } = [];
}

/// <summary>One step in the negotiation — an initial ask, a client counter, or a lawyer
/// counter-counter. Append-only, like <see cref="RequestStatusHistory"/>: the negotiation's
/// full history is exactly its revision list, nothing is ever edited in place.</summary>
public class OfferRevision : Entity<Guid>
{
    public Guid OfferId { get; set; }
    public decimal Amount { get; set; }
    public OfferProposedBy ProposedBy { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
