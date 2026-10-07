namespace LawPortal.Application.Payments;

/// <summary>
/// Splits a request's total into what a lawyer keeps, what the platform earns, and what's owed
/// to ZATCA. <see cref="Breakdown.Total"/> is always VAT-inclusive when VAT applies, matching
/// the VAT-inclusive-pricing convention already established for lawyer pricing in P2 — so VAT is
/// backed out of the total rather than added on top.
/// </summary>
public static class PaymentBreakdownCalculator
{
    public const decimal VatRate = 0.15m;

    public record Breakdown(
        decimal Total,
        decimal VatAmount,
        decimal SubtotalExVat,
        decimal CommissionAmount,
        decimal NetToLawyerAmount,
        bool IsVatApplicable);

    /// <summary>A platform-funded discount: the client pays <paramref name="gross"/> − <paramref name="discount"/>
    /// and VAT is backed out of what they actually pay, but the lawyer's net and the platform's
    /// commission are both computed on the full gross price — the gap is the platform's
    /// <see cref="DiscountExpense(Breakdown)"/>.</summary>
    public static Breakdown ComputeWithPlatformDiscount(decimal gross, decimal discount, bool isVatApplicable, decimal commissionPercentage)
    {
        var onGross = Compute(gross, isVatApplicable, commissionPercentage);
        if (discount <= 0) return onGross;

        var total = gross - discount;
        var vatAmount = isVatApplicable ? Math.Round(total * VatRate / (1 + VatRate), 2) : 0m;
        return new Breakdown(total, vatAmount, total - vatAmount, onGross.CommissionAmount, onGross.NetToLawyerAmount, isVatApplicable);
    }

    /// <summary>What the platform absorbs to keep the books balanced: VAT + lawyer net + commission
    /// minus what the client actually paid. Zero without a discount.</summary>
    public static decimal DiscountExpense(Breakdown b) => b.VatAmount + b.NetToLawyerAmount + b.CommissionAmount - b.Total;

    public static Breakdown Compute(decimal total, bool isVatApplicable, decimal commissionPercentage)
    {
        var vatAmount = isVatApplicable ? Math.Round(total * VatRate / (1 + VatRate), 2) : 0m;
        var subtotalExVat = total - vatAmount;
        var commissionAmount = Math.Round(subtotalExVat * commissionPercentage / 100m, 2);
        var netToLawyer = subtotalExVat - commissionAmount;
        return new Breakdown(total, vatAmount, subtotalExVat, commissionAmount, netToLawyer, isVatApplicable);
    }
}
