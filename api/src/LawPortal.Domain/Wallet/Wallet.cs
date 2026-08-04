using LawPortal.Domain.Common;
using LawPortal.Domain.Identity;

namespace LawPortal.Domain.Wallet;

public class Wallet : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = "SAR";

    public ICollection<WalletTransaction> Transactions { get; set; } = [];
}
