namespace LawPortal.Domain.Payments;

public enum PaymentPurpose
{
    RequestCheckout = 1,
    WalletTopUp = 2,
}

public enum PaymentStatus
{
    Initiated = 0,
    Paid = 10,
    Failed = 20,
    Refunded = 30,
    PartiallyRefunded = 31,
}

public enum RefundStatus
{
    Pending = 0,
    Completed = 10,
    Failed = 20,
}

public enum PayoutStatus
{
    Held = 0,
    Released = 10,
    /// <summary>The client got all their money back, so nothing is owed to the lawyer; never released.</summary>
    Cancelled = 20,
    /// <summary>The lawyer deleted their account before the work was completed: never released
    /// automatically; the money stays held until an admin decides.</summary>
    Suspended = 30,
}
