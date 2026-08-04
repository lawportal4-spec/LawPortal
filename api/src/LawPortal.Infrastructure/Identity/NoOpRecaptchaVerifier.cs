using LawPortal.Application.Common.Interfaces;

namespace LawPortal.Infrastructure.Identity;

/// <summary>Dev-only stand-in — no reCAPTCHA site/secret key exists yet. Always passes.
/// MUST be replaced with a real siteverify call before launch.</summary>
public class NoOpRecaptchaVerifier : IRecaptchaVerifier
{
    public Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
