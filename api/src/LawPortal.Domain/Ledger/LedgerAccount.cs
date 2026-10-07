namespace LawPortal.Domain.Ledger;

/// <summary>
/// A minimal marketplace chart of accounts — enough to hold escrow, commission, VAT, and wallet
/// liabilities under double-entry without a general-ledger product behind it.
/// </summary>
public enum LedgerAccount
{
    /// <summary>Asset — cash the payment gateway holds on our behalf pending settlement.</summary>
    ClearingGateway = 1,

    /// <summary>Liability — held for lawyers until a payout releases it.</summary>
    EscrowPayable = 2,

    /// <summary>Revenue — our cut of each transaction.</summary>
    CommissionRevenue = 3,

    /// <summary>Liability — VAT collected on behalf of ZATCA, owed until remitted.</summary>
    VatPayable = 4,

    /// <summary>Liability — client wallet balances funded but not yet spent.</summary>
    WalletLiability = 5,

    /// <summary>Revenue — lawyer subscription fees, the platform's own VAT-applicable supply
    /// (not a marketplace commission cut, so it never touches <see cref="EscrowPayable"/>).</summary>
    SubscriptionRevenue = 6,

    /// <summary>Expense — discount codes the platform funds on marketplace payments: the lawyer
    /// is paid on the full price, the client paid less, and this account absorbs the gap.</summary>
    DiscountExpense = 7,

    /// <summary>The one-off fee a newly approved lawyer pays before the portal opens — platform
    /// income, like <see cref="SubscriptionRevenue"/>.</summary>
    RegistrationFeeRevenue = 8,
}
