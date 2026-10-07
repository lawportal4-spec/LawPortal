using System.Security.Cryptography;
using System.Text;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments;
using LawPortal.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers;

/// <summary>Licence numbers as they're compared: Latin digits, no spaces or separators, upper case —
/// so the same licence typed as "44/1029", "441029" or "٤٤-١٠٢٩" is one licence.</summary>
public static class LicenseNumbers
{
    public static string Normalize(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw.Trim())
        {
            if (c is >= '٠' and <= '٩') sb.Append((char)('0' + (c - '٠')));      // Arabic-Indic
            else if (c is >= '۰' and <= '۹') sb.Append((char)('0' + (c - '۰'))); // Persian
            else if (char.IsWhiteSpace(c) || c is '/' or '-' or '.' or '_' or '\\') continue;
            else sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Sets the licence's number after checking no other lawyer holds it in any spelling.</summary>
    public static async Task AssignAsync(ILawPortalDbContext db, Domain.Identity.LawyerLicense license, string number, CancellationToken cancellationToken)
    {
        var key = Normalize(number);
        if (await db.LawyerLicenses.AnyAsync(
                l => l.Id != license.Id && (l.LicenseNumberKey == key || l.LicenseNumber == number), cancellationToken))
            throw new InvalidOperationException("This licence number is already registered.");
        license.LicenseNumber = number.Trim();
        license.LicenseNumberKey = key;
    }
}

/// <summary>Saudi national ID (starts with 1) or Iqama (starts with 2): 10 digits with a check digit.</summary>
public static class NationalIds
{
    public static string Normalize(string raw) => LicenseNumbers.Normalize(raw);

    public static bool IsValid(string? raw)
    {
        if (raw is null) return false;
        var id = Normalize(raw);
        if (id.Length != 10 || !id.All(char.IsAsciiDigit) || id[0] is not ('1' or '2')) return false;
        // Luhn-style: double the digits in odd positions (1st, 3rd, …), add the digits of each result.
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            var d = id[i] - '0';
            if (i % 2 == 0) { d *= 2; if (d > 9) d -= 9; }
            sum += d;
        }
        return sum % 10 == 0;
    }
}

/// <summary>One-way fingerprints of a deleted lawyer's identifiers, to recognise them if they sign
/// up again. Normalised first so formatting can't dodge the match.</summary>
public static class IdentityFingerprints
{
    public static string? Of(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("lp-fp:" + LicenseNumbers.Normalize(value).ToLowerInvariant())));

    /// <summary>The former lawyer account (if any) this sign-up matches that still owes money.</summary>
    public static async Task<Guid?> FindFormerDebtorAsync(
        ILawPortalDbContext db, string? phone, string? email, string? nationalId, CancellationToken cancellationToken)
    {
        var hashes = new[] { Of(phone), Of(email), Of(nationalId) }.Where(h => h is not null).ToList();
        if (hashes.Count == 0) return null;

        var candidates = await db.DeletedAccountFingerprints
            .Where(f => hashes.Contains(f.PhoneHash) || hashes.Contains(f.EmailHash) || hashes.Contains(f.NationalIdHash))
            .Select(f => f.LawyerProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var profileId in candidates)
        {
            if (await LawyerDebts.BalanceAsync(db, profileId, cancellationToken) > 0) return profileId;
        }
        return null;
    }
}
