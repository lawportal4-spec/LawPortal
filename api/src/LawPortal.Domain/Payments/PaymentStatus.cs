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
}
