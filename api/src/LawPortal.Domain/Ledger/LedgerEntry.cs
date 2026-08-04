using LawPortal.Domain.Common;

namespace LawPortal.Domain.Ledger;

/// <summary>
/// One leg of a double-entry posting. Append-only by convention — never updated or deleted once
/// written; a correction is a new, offsetting entry, not an edit. A posting is only ever created
/// as a balanced set (sum of debits == sum of credits) by <c>LedgerPostingService</c>.
/// </summary>
public class LedgerEntry : Entity<Guid>
{
    public LedgerAccount Account { get; set; }
    public bool IsDebit { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "SAR";

    /// <summary>What this posting relates to — "Payment", "Refund", or "Payout" — plus the id of
    /// that record, so every ledger row traces back to the transaction that caused it.</summary>
    public required string ReferenceType { get; set; }
    public Guid ReferenceId { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
