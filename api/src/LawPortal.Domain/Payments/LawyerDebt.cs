using LawPortal.Domain.Common;

namespace LawPortal.Domain.Payments;

public enum LawyerDebtEntryKind
{
    /// <summary>A refund given after the lawyer was paid; the lawyer bears their share.</summary>
    RefundAfterPayout = 1,
    /// <summary>Deducted from a later payout.</summary>
    PayoutOffset = 2,
    /// <summary>The lawyer paid it back by bank transfer, recorded by an admin.</summary>
    BankTransfer = 3,
    /// <summary>Moved in from a former account the same person left with a debt.</summary>
    TransferredIn = 4,
    /// <summary>Moved out to that person's new account.</summary>
    TransferredOut = 5,
}

/// <summary>One movement on a lawyer's debt to the platform. The balance is the sum of
/// <see cref="Amount"/>: positive adds to the debt, negative pays it down. Append-only.</summary>
public class LawyerDebtEntry : Entity<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public LawyerDebtEntryKind Kind { get; set; }
    public decimal Amount { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? RefundId { get; set; }
    public Guid? PayoutId { get; set; }
    /// <summary>Bank transfer reference, or the other profile for a transfer.</summary>
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Refund and debt-collection policy. A single row (<see cref="SingletonId"/>), edited by
/// admins; when the row doesn't exist yet the defaults apply.</summary>
public class RefundPolicySetting : Entity<int>
{
    public const int SingletonId = 1;

    /// <summary>Days after the lawyer's payout is released during which the client can still be refunded.</summary>
    public int RefundWindowDays { get; set; } = 30;
    /// <summary>How often a lawyer who owes money gets an automatic reminder.</summary>
    public int DebtReminderIntervalDays { get; set; } = 7;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
