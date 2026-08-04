using LawPortal.Domain.Common;

namespace LawPortal.Domain.Billing;

/// <summary>
/// Client-facing invoice for a paid request. Each lawyer is the merchant of record for the
/// underlying legal service — VAT applicability and the seller VAT number follow the assigned
/// lawyer's registration status (<see cref="Identity.LawyerProfile.IsVatRegistered"/>), matching
/// how P2 already ties displayed pricing to that same per-lawyer flag. The platform's own
/// commission is a separate internal ledger entry (<see cref="Ledger.LedgerAccount.CommissionRevenue"/>),
/// not a second invoice document — a lawyer-facing commission invoice belongs with the lawyer's
/// own financial views (P6/P10), not this pass.
///
/// <see cref="QrPayloadBase64"/> is a ZATCA Phase 1 (simplified tax invoice) TLV QR payload —
/// seller name, VAT number, timestamp, total, VAT amount. Phase 2 (cryptographic invoice
/// stamping and real-time clearance through ZATCA's API) needs a real ZATCA merchant
/// onboarding this project doesn't have yet; deferred, same as the Moyasar account gap below.
/// </summary>
public class Invoice : Entity<Guid>
{
    public Guid PaymentId { get; set; }
    public Guid ServiceRequestId { get; set; }

    public required string Number { get; set; }
    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public bool IsVatApplicable { get; set; }

    public string SellerNameAr { get; set; } = "بوابة القانون";
    public string SellerNameEn { get; set; } = "Law Portal";
    public string? SellerVatNumber { get; set; }

    public required string QrPayloadBase64 { get; set; }
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
}
