using System.Security.Cryptography;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LawPortal.Application.Auth;

/// <summary>Issues and checks phone OTPs for every flow that needs one (client sign-in, lawyer
/// registration). Challenges are scoped by <see cref="OtpPurpose"/> so a code sent for one flow
/// can never be redeemed by the other.</summary>
public class OtpService(ILawPortalDbContext db, IOtpSender otpSender, IHostEnvironment environment)
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ExpiryWindow = TimeSpan.FromMinutes(5);
    private const int MaxRequestsPerDay = 10;

    /// <summary>Dev-only universal code so QA/local testing doesn't need access to the server's
    /// OTP log. Still requires a real, unexpired challenge (i.e. a code was actually requested) —
    /// only the code-matching step is skipped. Never honoured outside Development.</summary>
    public const string DevBypassCode = "000000";

    public async Task IssueAsync(string phoneE164, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-1);
        var recentChallenges = await db.OtpChallenges
            .Where(o => o.PhoneE164 == phoneE164 && o.Purpose == purpose && o.CreatedAtUtc > since)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (recentChallenges.Count >= MaxRequestsPerDay)
            throw new InvalidOperationException("Too many OTP requests for this number today. Try again tomorrow.");

        var latest = recentChallenges.FirstOrDefault();
        if (latest is not null && DateTime.UtcNow - latest.CreatedAtUtc < ResendCooldown)
            throw new InvalidOperationException("Please wait before requesting another code.");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        db.OtpChallenges.Add(new OtpChallenge
        {
            Id = Guid.NewGuid(),
            PhoneE164 = phoneE164,
            CodeHash = HashCode(phoneE164, code),
            Purpose = purpose,
            ExpiresAtUtc = DateTime.UtcNow.Add(ExpiryWindow),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        await otpSender.SendAsync(phoneE164, code, cancellationToken);
    }

    /// <summary>Marks the latest matching challenge consumed, or throws. Does not save on success —
    /// the caller saves together with whatever the verification unlocks.</summary>
    public async Task VerifyAsync(string phoneE164, string code, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var challenge = await MatchAsync(phoneE164, code, purpose, cancellationToken);
        challenge.ConsumedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Checks a code without using it up — for flows that confirm the code on one screen
    /// and act on it on the next (password reset). Wrong guesses still count towards the lock,
    /// and the final step must still call <see cref="VerifyAsync"/>.</summary>
    public async Task CheckAsync(string phoneE164, string code, OtpPurpose purpose, CancellationToken cancellationToken) =>
        await MatchAsync(phoneE164, code, purpose, cancellationToken);

    private async Task<OtpChallenge> MatchAsync(string phoneE164, string code, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var challenge = await db.OtpChallenges
            .Where(o => o.PhoneE164 == phoneE164 && o.Purpose == purpose && o.ConsumedAtUtc == null)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null || challenge.IsExpired)
            throw new InvalidOperationException("No active code for this number. Request a new one.");

        if (challenge.IsLocked)
            throw new InvalidOperationException("Too many incorrect attempts. Request a new code.");

        var isDevBypass = environment.IsDevelopment() && code == DevBypassCode;
        if (!isDevBypass && challenge.CodeHash != HashCode(phoneE164, code))
        {
            challenge.Attempts++;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Incorrect code.");
        }

        return challenge;
    }

    private static string HashCode(string phone, string code) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{phone}:{code}")));
}
