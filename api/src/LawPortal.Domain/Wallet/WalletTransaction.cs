using LawPortal.Domain.Common;

namespace LawPortal.Domain.Wallet;

public enum WalletTransactionType
{
    TopUp = 1,
    Payment = 2,
    Refund = 3,
}

/// <summary>Amount is always positive; <see cref="Type"/> determines the balance direction —
/// TopUp and Refund credit the wallet, Payment debits it.</summary>
public class WalletTransaction : Entity<Guid>
{
    public Guid WalletId { get; set; }
    public Wallet? Wallet { get; set; }

    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid? PaymentId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
