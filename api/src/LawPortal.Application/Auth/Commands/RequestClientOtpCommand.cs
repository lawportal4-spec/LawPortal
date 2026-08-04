using System.Security.Cryptography;
using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>Phone must already be E.164 (+9665XXXXXXXX) — the client formats it before sending.</summary>
public record RequestClientOtpCommand(string PhoneE164, string RecaptchaToken) : IRequest<Unit>;

public class RequestClientOtpValidator : AbstractValidator<RequestClientOtpCommand>
{
    public RequestClientOtpValidator()
    {
        RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$")
            .WithMessage("Phone must be a Saudi mobile number in E.164 format, e.g. +966501234567.");
        RuleFor(x => x.RecaptchaToken).NotEmpty();
    }
}

public class RequestClientOtpHandler(
    ILawPortalDbContext db,
    IOtpSender otpSender,
    IRecaptchaVerifier recaptcha)
    : IRequestHandler<RequestClientOtpCommand, Unit>
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ExpiryWindow = TimeSpan.FromMinutes(5);
    private const int MaxRequestsPerDay = 10;

    public async Task<Unit> Handle(RequestClientOtpCommand request, CancellationToken cancellationToken)
    {
        if (!await recaptcha.VerifyAsync(request.RecaptchaToken, cancellationToken))
            throw new ValidationException("reCAPTCHA verification failed.");

        var since = DateTime.UtcNow.AddDays(-1);
        var recentChallenges = await db.OtpChallenges
            .Where(o => o.PhoneE164 == request.PhoneE164 && o.CreatedAtUtc > since)
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
            PhoneE164 = request.PhoneE164,
            CodeHash = HashCode(request.PhoneE164, code),
            Purpose = OtpPurpose.Login,
            ExpiresAtUtc = DateTime.UtcNow.Add(ExpiryWindow),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        await otpSender.SendAsync(request.PhoneE164, code, cancellationToken);

        return Unit.Value;
    }

    internal static string HashCode(string phone, string code) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{phone}:{code}")));
}
