using LawPortal.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments;

/// <summary>Mirrors <see cref="Requests.RequestNumberGenerator"/>'s accepted small race window
/// in exchange for not needing new sequence infrastructure.</summary>
public static class PaymentNumberGenerator
{
    public static async Task<string> NextPaymentNumberAsync(ILawPortalDbContext db, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var countThisYear = await db.Payments.CountAsync(p => p.CreatedAtUtc.Year == year, cancellationToken);
        return $"PAY-{year}-{(countThisYear + 1):D6}";
    }

    public static async Task<string> NextInvoiceNumberAsync(ILawPortalDbContext db, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var countThisYear = await db.Invoices.CountAsync(i => i.IssuedAtUtc.Year == year, cancellationToken);
        return $"INV-{year}-{(countThisYear + 1):D6}";
    }
}
