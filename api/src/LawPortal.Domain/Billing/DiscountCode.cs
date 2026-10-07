using LawPortal.Domain.Common;

namespace LawPortal.Domain.Billing;

public enum DiscountKind
{
    Percentage = 1,
    Fixed = 2,
}

/// <summary>Every path where money is charged and a code may apply. Wallet top-ups are
/// deliberately absent — a discounted top-up would be free money.</summary>
[Flags]
public enum DiscountScope
{
    None = 0,
    InstantConsultation = 1,
    ScheduledConsultation = 2,
    WrittenConsultation = 4,
    BiddingRequest = 8,
    CatalogService = 16,
    LawyerSubscription = 32,
    LawyerRegistrationFee = 64,
}

/// <summary>An admin-defined promotion code. Amounts are in SAR and apply to the price the payer
/// would otherwise see (VAT-inclusive for marketplace prices, before VAT for platform fees).</summary>
public class DiscountCode : AggregateRoot<Guid>
{
    /// <summary>Stored upper-case; matched case-insensitively.</summary>
    public required string Code { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    public DiscountKind Kind { get; set; } = DiscountKind.Percentage;
    /// <summary>Percent (0–100) for <see cref="DiscountKind.Percentage"/>, SAR for Fixed.</summary>
    public decimal Value { get; set; }
    /// <summary>Upper bound on a percentage discount, e.g. "20% up to 100 SAR".</summary>
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinAmount { get; set; }

    public DiscountScope Scopes { get; set; }
    public int? UsageLimit { get; set; }
    public int? PerUserLimit { get; set; }
    public bool FirstPaymentOnly { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }

    public ICollection<DiscountRedemption> Redemptions { get; set; } = [];
}

public enum DiscountRedemptionStatus
{
    /// <summary>Held at checkout so parallel checkouts can't oversell a limited code; stops
    /// counting once stale (abandoned checkout).</summary>
    Pending = 1,
    Confirmed = 2,
    Released = 3,
}

public class DiscountRedemption : Entity<Guid>
{
    public Guid DiscountCodeId { get; set; }
    public DiscountCode? DiscountCode { get; set; }

    public Guid UserId { get; set; }
    public DiscountScope Scope { get; set; }
    /// <summary>The Payment / SubscriptionInvoice / fee invoice the code was applied to.</summary>
    public Guid ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public DiscountRedemptionStatus Status { get; set; } = DiscountRedemptionStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
