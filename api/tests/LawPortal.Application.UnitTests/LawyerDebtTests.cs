using LawPortal.Application.Lawyers;
using LawPortal.Application.Payments;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;

namespace LawPortal.Application.UnitTests;

public class LawyerDebtTests
{
    [Theory]
    [InlineData("44/1029", "441029")]
    [InlineData(" 44-1029 ", "441029")]
    [InlineData("٤٤/١٠٢٩", "441029")]
    [InlineData("۴۴.۱۰۲۹", "441029")]
    [InlineData("ab 12", "AB12")]
    public void LicenseNumbers_AreComparedNormalised(string raw, string expected) =>
        Assert.Equal(expected, LicenseNumbers.Normalize(raw));

    [Theory]
    [InlineData("1234567897", true)]   // citizen, valid check digit
    [InlineData("2000000006", true)]   // iqama
    [InlineData("١٢٣٤٥٦٧٨٩٧", true)]   // Arabic digits
    [InlineData("1234567890", false)]  // wrong check digit
    [InlineData("3234567897", false)]  // must start with 1 or 2
    [InlineData("123456789", false)]
    [InlineData(null, false)]
    public void NationalIds_CheckDigit(string? id, bool valid) => Assert.Equal(valid, NationalIds.IsValid(id));

    [Fact]
    public void PayoutWithDebtOffset_PaysTheRest_AndBalances()
    {
        var payout = new Payout { Id = Guid.NewGuid(), Amount = 200m, DebtOffset = 45m };
        var entries = LedgerPostingService.PostPayout(payout);

        Assert.Equal(entries.Where(e => e.IsDebit).Sum(e => e.Amount), entries.Where(e => !e.IsDebit).Sum(e => e.Amount));
        Assert.Equal(155m, entries.Single(e => e.Account == LedgerAccount.ClearingGateway).Amount);
        Assert.Equal(45m, entries.Single(e => e.Account == LedgerAccount.LawyerReceivable).Amount);
    }

    [Fact]
    public void PayoutWithoutDebt_HasNoReceivableLine() =>
        Assert.DoesNotContain(LedgerPostingService.PostPayout(new Payout { Id = Guid.NewGuid(), Amount = 85m }),
            e => e.Account == LedgerAccount.LawyerReceivable);

    [Theory]
    [InlineData(LedgerAccount.LawyerReceivable)]
    [InlineData(LedgerAccount.RefundLossExpense)]
    public void RefundAfterPayout_TakesLawyerShareFromTheChosenAccount(LedgerAccount account)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(), Number = "PAY-T", Purpose = PaymentPurpose.RequestCheckout,
            Total = 115m, VatAmount = 15m, CommissionAmount = 15m, NetToLawyerAmount = 85m, IsVatApplicable = true,
        };
        var refund = new Refund { Id = Guid.NewGuid(), Amount = 115m, Reason = RefundReason.QualityComplaint };
        var entries = LedgerPostingService.PostRefund(payment, refund, wasWalletFunded: false, account);

        Assert.Equal(entries.Where(e => e.IsDebit).Sum(e => e.Amount), entries.Where(e => !e.IsDebit).Sum(e => e.Amount));
        Assert.Equal(85m, entries.Single(e => e.Account == account).Amount);
        Assert.DoesNotContain(entries, e => e.Account == LedgerAccount.EscrowPayable);
    }
}
