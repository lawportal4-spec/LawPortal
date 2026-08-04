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

    public static Breakdown Compute(decimal total, bool isVatApplicable, decimal commissionPercentage)
    {
        var vatAmount = isVatApplicable ? Math.Round(total * VatRate / (1 + VatRate), 2) : 0m;
        var subtotalExVat = total - vatAmount;
        var commissionAmount = Math.Round(subtotalExVat * commissionPercentage / 100m, 2);
        var netToLawyer = subtotalExVat - commissionAmount;
        return new Breakdown(total, vatAmount, subtotalExVat, commissionAmount, netToLawyer, isVatApplicable);
    }
}
