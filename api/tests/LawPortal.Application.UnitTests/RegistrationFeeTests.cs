using LawPortal.Application.Lawyers.Onboarding;
using LawPortal.Domain.Billing;

namespace LawPortal.Application.UnitTests;

public class RegistrationFeeTests
{
    [Theory]
    [InlineData(500, 0, 500, 75, 575)]      // the agreed fee: 500 + 15% VAT
    [InlineData(500, 100, 400, 60, 460)]    // 20% code: discount comes off before VAT
    [InlineData(333.33, 0, 333.33, 50, 383.33)]
    public void PricesBeforeVat(decimal baseAmount, decimal discount, decimal subtotal, decimal vat, decimal total)
    {
        var invoice = new LawyerRegistrationFeeInvoice { Number = "REG-TEST" };
        LawyerOnboarding.Price(invoice, baseAmount, discount);
        Assert.Equal((subtotal, vat, total), (invoice.SubtotalExVat, invoice.VatAmount, invoice.Total));
        Assert.Equal(invoice.Total, invoice.SubtotalExVat + invoice.VatAmount);
    }
}
