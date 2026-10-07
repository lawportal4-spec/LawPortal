using LawPortal.Domain.Billing;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Subscriptions;

namespace LawPortal.Application.Payments;

/// <summary>
/// The only place that writes <see cref="LedgerEntry"/> rows. Every method returns a fully
/// balanced posting (sum of debits == sum of credits) — <see cref="AssertBalanced"/> is a cheap
/// runtime invariant check, not a substitute for the accounting logic being correct, but it
/// catches an arithmetic slip immediately instead of silently corrupting the books.
/// </summary>
public static class LedgerPostingService
{
    public static IReadOnlyList<LedgerEntry> PostCardCheckoutSuccess(Payment payment) =>
        AssertBalanced(BuildCheckoutEntries(payment, LedgerAccount.ClearingGateway));

    public static IReadOnlyList<LedgerEntry> PostWalletCheckoutSuccess(Payment payment) =>
        AssertBalanced(BuildCheckoutEntries(payment, LedgerAccount.WalletLiability));

    private static List<LedgerEntry> BuildCheckoutEntries(Payment payment, LedgerAccount fundingAccount)
    {
        var entries = new List<LedgerEntry>
        {
            Entry(fundingAccount, isDebit: true, payment.Total, payment.Id, "Payment", "Client payment collected"),
        };

        if (payment.IsVatApplicable && payment.VatAmount > 0)
            entries.Add(Entry(LedgerAccount.VatPayable, isDebit: false, payment.VatAmount, payment.Id, "Payment", "VAT collected on behalf of ZATCA"));

        entries.Add(Entry(LedgerAccount.EscrowPayable, isDebit: false, payment.NetToLawyerAmount, payment.Id, "Payment", "Held in escrow for the lawyer"));
        entries.Add(Entry(LedgerAccount.CommissionRevenue, isDebit: false, payment.CommissionAmount, payment.Id, "Payment", "Platform commission"));

        if (DiscountExpenseOf(payment) is > 0 and var discountExpense)
            entries.Add(Entry(LedgerAccount.DiscountExpense, isDebit: true, discountExpense, payment.Id, "Payment", "Discount code funded by the platform"));
        return entries;
    }

    public static IReadOnlyList<LedgerEntry> PostWalletTopUp(Payment payment) => AssertBalanced(
    [
        Entry(LedgerAccount.ClearingGateway, isDebit: true, payment.Total, payment.Id, "Payment", "Wallet top-up collected"),
        Entry(LedgerAccount.WalletLiability, isDebit: false, payment.Total, payment.Id, "Payment", "Wallet balance credited"),
    ]);

    /// <summary>A subscription fee is the platform's own VAT-applicable supply, not a
    /// marketplace transaction — no escrow, no commission split, pure platform revenue.</summary>
    public static IReadOnlyList<LedgerEntry> PostSubscriptionPayment(SubscriptionInvoice invoice)
    {
        var entries = new List<LedgerEntry>
        {
            Entry(LedgerAccount.ClearingGateway, isDebit: true, invoice.Total, invoice.Id, "SubscriptionInvoice", "Subscription fee collected"),
        };
        if (invoice.VatAmount > 0)
            entries.Add(Entry(LedgerAccount.VatPayable, isDebit: false, invoice.VatAmount, invoice.Id, "SubscriptionInvoice", "VAT collected on behalf of ZATCA"));
        entries.Add(Entry(LedgerAccount.SubscriptionRevenue, isDebit: false, invoice.SubtotalExVat, invoice.Id, "SubscriptionInvoice", "Subscription revenue"));
        return AssertBalanced(entries);
    }

    public static IReadOnlyList<LedgerEntry> PostRegistrationFee(LawyerRegistrationFeeInvoice invoice)
    {
        var entries = new List<LedgerEntry>
        {
            Entry(LedgerAccount.ClearingGateway, isDebit: true, invoice.Total, invoice.Id, "RegistrationFeeInvoice", "Registration fee collected"),
            Entry(LedgerAccount.VatPayable, isDebit: false, invoice.VatAmount, invoice.Id, "RegistrationFeeInvoice", "VAT collected on behalf of ZATCA"),
            Entry(LedgerAccount.RegistrationFeeRevenue, isDebit: false, invoice.SubtotalExVat, invoice.Id, "RegistrationFeeInvoice", "Registration fee revenue"),
        };
        return AssertBalanced(entries);
    }

    public static IReadOnlyList<LedgerEntry> PostPayout(Payout payout) => AssertBalanced(
    [
        Entry(LedgerAccount.EscrowPayable, isDebit: true, payout.Amount, payout.Id, "Payout", "Escrow released to lawyer"),
        Entry(LedgerAccount.ClearingGateway, isDebit: false, payout.Amount, payout.Id, "Payout", "Cash paid out to lawyer"),
    ]);

    /// <summary>Reverses a proportional slice of the original checkout posting. Only valid while
    /// the associated payout (if any) is still <see cref="PayoutStatus.Held"/> — once released,
    /// the money has left escrow and a refund needs a clawback/dispute path this pass doesn't
    /// build (see <c>RefundPaymentCommand</c>'s guard).</summary>
    public static IReadOnlyList<LedgerEntry> PostRefund(Payment payment, Refund refund, bool wasWalletFunded)
    {
        var ratio = refund.Amount / payment.Total;
        var vatPortion = Math.Round(payment.VatAmount * ratio, 2);
        var commissionPortion = Math.Round(payment.CommissionAmount * ratio, 2);
        var netToLawyerPortion = Math.Round(payment.NetToLawyerAmount * ratio, 2);
        var discountPortion = Math.Round(DiscountExpenseOf(payment) * ratio, 2);
        // Whichever portion rounding leaves over lands back on the escrow leg, which is the
        // largest and least visible amount to a client reading their refund confirmation.
        netToLawyerPortion += refund.Amount - (vatPortion + commissionPortion + netToLawyerPortion - discountPortion);

        var entries = new List<LedgerEntry>();
        if (vatPortion > 0)
            entries.Add(Entry(LedgerAccount.VatPayable, isDebit: true, vatPortion, refund.Id, "Refund", "VAT reversed"));
        entries.Add(Entry(LedgerAccount.EscrowPayable, isDebit: true, netToLawyerPortion, refund.Id, "Refund", "Escrow reversed"));
        entries.Add(Entry(LedgerAccount.CommissionRevenue, isDebit: true, commissionPortion, refund.Id, "Refund", "Commission reversed"));
        if (discountPortion > 0)
            entries.Add(Entry(LedgerAccount.DiscountExpense, isDebit: false, discountPortion, refund.Id, "Refund", "Discount expense reversed"));

        var returnAccount = wasWalletFunded ? LedgerAccount.WalletLiability : LedgerAccount.ClearingGateway;
        entries.Add(Entry(returnAccount, isDebit: false, refund.Amount, refund.Id, "Refund", "Refunded to client"));

        return AssertBalanced(entries);
    }

    /// <summary>Recovered from the snapshot rather than stored: a discounted payment pays the
    /// lawyer and the platform on the gross price, so its legs exceed what the client paid.</summary>
    private static decimal DiscountExpenseOf(Payment payment) =>
        payment.Purpose == PaymentPurpose.RequestCheckout
            ? Math.Max(0, payment.VatAmount + payment.NetToLawyerAmount + payment.CommissionAmount - payment.Total)
            : 0;

    private static LedgerEntry Entry(LedgerAccount account, bool isDebit, decimal amount, Guid referenceId, string referenceType, string description) => new()
    {
        Id = Guid.NewGuid(),
        Account = account,
        IsDebit = isDebit,
        Amount = amount,
        ReferenceType = referenceType,
        ReferenceId = referenceId,
        Description = description,
    };

    private static List<LedgerEntry> AssertBalanced(List<LedgerEntry> entries)
    {
        var balance = entries.Sum(e => e.IsDebit ? e.Amount : -e.Amount);
        if (balance != 0m)
            throw new InvalidOperationException($"Ledger posting is not balanced (off by {balance}) — refusing to write it.");
        return entries;
    }
}
