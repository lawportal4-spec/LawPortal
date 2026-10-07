using LawPortal.Application.Payments;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Requests;

namespace LawPortal.Application.UnitTests;

public class DiscountMathTests
{
    private static DiscountCode Code(DiscountKind kind, decimal value, decimal? cap = null) =>
        new() { Code = "X", Kind = kind, Value = value, MaxDiscountAmount = cap };

    [Theory]
    [InlineData(DiscountKind.Percentage, 20, null, 300, 60)]
    [InlineData(DiscountKind.Percentage, 20, 50, 300, 50)]    // capped
    [InlineData(DiscountKind.Fixed, 75, null, 300, 75)]
    [InlineData(DiscountKind.Fixed, 500, null, 300, 300)]    // capped at the price: total 0, never negative
    [InlineData(DiscountKind.Percentage, 100, null, 200, 200)]
    [InlineData(DiscountKind.Percentage, 15, null, 99.99, 15)] // rounded to halalas
    public void ComputeAmount(DiscountKind kind, decimal value, int? cap, decimal gross, decimal expected) =>
        Assert.Equal(expected, DiscountService.ComputeAmount(Code(kind, value, (decimal?)cap), gross));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlatformFundedDiscount_KeepsLawyerShare_AndLedgerBalances(bool vat)
    {
        var full = PaymentBreakdownCalculator.Compute(575, vat, 20);
        var discounted = PaymentBreakdownCalculator.ComputeWithPlatformDiscount(575, 100, vat, 20);

        Assert.Equal(475, discounted.Total);
        Assert.Equal(full.NetToLawyerAmount, discounted.NetToLawyerAmount);
        Assert.Equal(full.CommissionAmount, discounted.CommissionAmount);
        Assert.Equal(vat ? Math.Round(475 * 0.15m / 1.15m, 2) : 0, discounted.VatAmount);
        // Charged + platform-funded expense = every credit leg.
        Assert.Equal(discounted.VatAmount + discounted.NetToLawyerAmount + discounted.CommissionAmount,
            discounted.Total + PaymentBreakdownCalculator.DiscountExpense(discounted));
    }

    [Fact]
    public void NoDiscount_MatchesPlainBreakdown()
    {
        Assert.Equal(PaymentBreakdownCalculator.Compute(300, true, 15), PaymentBreakdownCalculator.ComputeWithPlatformDiscount(300, 0, true, 15));
        Assert.Equal(0, PaymentBreakdownCalculator.DiscountExpense(PaymentBreakdownCalculator.Compute(300, true, 15)));
    }

    [Fact]
    public void ScopeFollowsRequestKind()
    {
        Assert.Equal(DiscountScope.InstantConsultation, DiscountService.ScopeOf(new ConsultationRequest { Number = "1", ConsultationType = ConsultationType.Instant }));
        Assert.Equal(DiscountScope.ScheduledConsultation, DiscountService.ScopeOf(new ConsultationRequest { Number = "1", ConsultationType = ConsultationType.Scheduled }));
        Assert.Equal(DiscountScope.WrittenConsultation, DiscountService.ScopeOf(new ConsultationRequest { Number = "1", ConsultationType = ConsultationType.Written }));
    }

    [Fact]
    public void FullyDiscountedOrder_PlatformFundsTheLawyer_AndLedgerBalances()
    {
        var b = PaymentBreakdownCalculator.ComputeWithPlatformDiscount(300, 300, true, 15);
        Assert.Equal(0, b.Total);
        Assert.Equal(0, b.VatAmount);
        Assert.Equal(PaymentBreakdownCalculator.Compute(300, true, 15).NetToLawyerAmount, b.NetToLawyerAmount);
        Assert.Equal(b.NetToLawyerAmount + b.CommissionAmount, PaymentBreakdownCalculator.DiscountExpense(b));
    }
}
