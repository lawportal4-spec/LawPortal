using System.Globalization;
using System.Text;

namespace LawPortal.Application.Payments;

/// <summary>
/// ZATCA Phase 1 (simplified tax invoice) QR payload — a TLV-encoded, Base64 byte sequence
/// carrying seller name, VAT number, timestamp, total, and VAT amount, per the e-invoicing
/// regulation's five required tags. Phase 2 (cryptographic invoice stamping and real-time
/// clearance through ZATCA's own API) needs a real ZATCA merchant onboarding this project
/// doesn't have — same gap as the Moyasar account. This builder needs no external service and
/// is fully verifiable offline (decode the Base64, walk the TLV, read the tags back).
/// </summary>
public static class ZatcaQrCodeBuilder
{
    public static string Build(string sellerName, string vatNumber, DateTime issuedAtUtc, decimal total, decimal vatAmount)
    {
        var bytes = new List<byte>();
        AppendTag(bytes, 1, sellerName);
        AppendTag(bytes, 2, vatNumber);
        AppendTag(bytes, 3, issuedAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
        AppendTag(bytes, 4, total.ToString("F2", CultureInfo.InvariantCulture));
        AppendTag(bytes, 5, vatAmount.ToString("F2", CultureInfo.InvariantCulture));
        return Convert.ToBase64String([.. bytes]);
    }

    private static void AppendTag(List<byte> bytes, byte tag, string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        bytes.Add(tag);
        bytes.Add((byte)valueBytes.Length);
        bytes.AddRange(valueBytes);
    }
}
