using System.Net;
using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Payments;
using LawPortal.Application.Payments.Dtos;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Lawyers.Onboarding;

/// <summary>What an approved-but-not-yet-active lawyer still has to do, in order.</summary>
public enum OnboardingStep
{
    VerifyEmail,
    PayFee,
}

/// <summary>
/// After an admin approves the licence the portal stays closed until the lawyer (1) follows the
/// "تفعيل الآن" link emailed to them and (2) pays the registration fee (when enabled). Only then
/// are they activated: <c>User.Status = Active</c> and <c>LawyerProfile.IsVerified</c>, which is
/// what makes them bookable.
/// </summary>
public static class LawyerOnboarding
{
    public static readonly TimeSpan EmailLinkLifetime = TimeSpan.FromHours(72);

    public static async Task<RegistrationFeeSetting> GetFeeSettingAsync(ILawPortalDbContext db, CancellationToken cancellationToken) =>
        await db.RegistrationFeeSettings.FirstOrDefaultAsync(s => s.Id == RegistrationFeeSetting.SingletonId, cancellationToken)
        ?? new RegistrationFeeSetting { Id = RegistrationFeeSetting.SingletonId };

    /// <summary>null once nothing is left (or the lawyer was never in onboarding).</summary>
    public static OnboardingStep? StepOf(LawyerProfile lawyer)
    {
        if (lawyer.User!.Status != UserStatus.PendingVerification || lawyer.License?.VerificationStatus != LicenseVerificationStatus.Approved)
            return null;
        if (!lawyer.User.IsEmailVerified) return OnboardingStep.VerifyEmail;
        return OnboardingStep.PayFee;
    }

    public static void Activate(LawyerProfile lawyer)
    {
        lawyer.IsVerified = true;
        lawyer.User!.Status = UserStatus.Active;
    }

    /// <summary>Called once the email is verified: activates straight away when no fee is due.</summary>
    public static async Task<OnboardingStep?> AdvanceAfterEmailAsync(ILawPortalDbContext db, LawyerProfile lawyer, CancellationToken cancellationToken)
    {
        var fee = await GetFeeSettingAsync(db, cancellationToken);
        var paid = await db.RegistrationFeeInvoices.AnyAsync(
            i => i.LawyerProfileId == lawyer.Id && i.Status == RegistrationFeeInvoiceStatus.Paid, cancellationToken);
        if (fee.IsEnabled && !paid) return OnboardingStep.PayFee;
        Activate(lawyer);
        return null;
    }

    /// <summary>Issues a fresh link (earlier unused ones stop working) and emails it.</summary>
    public static async Task SendVerificationEmailAsync(
        ILawPortalDbContext db, ITokenService tokenService, IEmailSender emailSender, IConfiguration configuration,
        User user, string fullName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) throw new InvalidOperationException("This account has no email address.");

        var link = await IssueEmailLinkAsync(db, tokenService, configuration, user.Id, newEmail: null, cancellationToken);
        var name = WebUtility.HtmlEncode(fullName);
        var body = $"""
            <div dir="rtl" style="font-family: Tahoma, Arial, sans-serif; max-width: 560px; margin: auto; color: #1f2933; line-height: 1.8;">
              <h2 style="color: #0f5c45;">تأكيد البريد الإلكتروني</h2>
              <p>مرحبًا أ. {name}،</p>
              <p>شكرًا لتسجيلك في بوابة القانون. تم قبول طلب انضمامك، ولإكمال عملية التسجيل وتفعيل حسابك كمحامٍ معتمد، يُرجى الضغط على الزر أدناه:</p>
              <p style="text-align: center; margin: 28px 0;">
                <a href="{link}" style="background: #0f5c45; color: #fff; padding: 12px 32px; border-radius: 8px; text-decoration: none; font-weight: bold;">تفعيل الآن</a>
              </p>
              <p style="font-size: 13px; color: #6b7280;">ينتهي هذا الرابط خلال 72 ساعة. إذا لم تطلب التسجيل في بوابة القانون فتجاهل هذه الرسالة.</p>
            </div>
            """;
        await emailSender.SendAsync(user.Email, "تأكيد البريد الإلكتروني في بوابة القانون", body, cancellationToken);
    }

    /// <summary>A fresh "/verify-email" link (earlier unused ones stop working). With
    /// <paramref name="newEmail"/>, following it switches the account to that address.</summary>
    public static async Task<string> IssueEmailLinkAsync(
        ILawPortalDbContext db, ITokenService tokenService, IConfiguration configuration, Guid userId, string? newEmail, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var open = await db.EmailVerificationTokens.Where(t => t.UserId == userId && t.UsedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var old in open) old.UsedAtUtc = now;

        var raw = tokenService.CreateRefreshTokenRaw();
        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenService.HashToken(raw),
            ExpiresAtUtc = now.Add(EmailLinkLifetime),
            NewEmail = newEmail,
        });
        await db.SaveChangesAsync(cancellationToken);

        var baseUrl = (configuration["Payments:LawyerBaseUrl"] ?? "http://localhost:5174").TrimEnd('/');
        return $"{baseUrl}/verify-email?token={Uri.EscapeDataString(raw)}";
    }

    /// <summary>The fee is stated before VAT; the discount comes off that, then 15% VAT is added.</summary>
    public static void Price(LawyerRegistrationFeeInvoice invoice, decimal baseAmount, decimal discount)
    {
        invoice.BaseAmount = baseAmount;
        invoice.DiscountAmount = discount;
        invoice.SubtotalExVat = baseAmount - discount;
        invoice.VatAmount = Math.Round(invoice.SubtotalExVat * PaymentBreakdownCalculator.VatRate, 2);
        invoice.Total = invoice.SubtotalExVat + invoice.VatAmount;
    }
}

// ── Email verification ───────────────────────────────────────────────────────────────────────

/// <param name="NextStep">"PayFee", or null when the account is now active.</param>
public record LawyerEmailVerifiedDto(string FullName, string? NextStep);

/// <summary>Public: the token in the emailed link is the proof. Using it twice is harmless — a
/// lawyer who opens the link again just sees where they stand.</summary>
public record VerifyLawyerEmailCommand(string Token) : IRequest<LawyerEmailVerifiedDto>;

public class VerifyLawyerEmailHandler(ILawPortalDbContext db, ITokenService tokenService, IAuditLogger auditLogger)
    : IRequestHandler<VerifyLawyerEmailCommand, LawyerEmailVerifiedDto>
{
    public async Task<LawyerEmailVerifiedDto> Handle(VerifyLawyerEmailCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashToken(request.Token);
        var token = await db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null || token.ExpiresAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("This activation link is invalid or has expired.");

        var lawyer = await db.LawyerProfiles.Include(l => l.User).Include(l => l.License)
            .FirstAsync(l => l.UserId == token.UserId, cancellationToken);

        if (token.NewEmail is { } newEmail)
            return await ChangeEmailAsync(token, lawyer, newEmail, cancellationToken);

        // A newer link superseded this one, or it was already used: fine if the email is verified.
        if (token.UsedAtUtc is not null && !lawyer.User!.IsEmailVerified)
            throw new InvalidOperationException("This activation link is invalid or has expired.");

        token.UsedAtUtc ??= DateTime.UtcNow;
        lawyer.User!.IsEmailVerified = true;
        var next = lawyer.User.Status == UserStatus.PendingVerification
            ? await LawyerOnboarding.AdvanceAfterEmailAsync(db, lawyer, cancellationToken)
            : null;

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("LawyerEmailVerified", nameof(LawyerProfile), lawyer.Id.ToString(), cancellationToken: cancellationToken);
        return new LawyerEmailVerifiedDto(lawyer.FullName, next?.ToString());
    }

    /// <summary>The link from "change my email": proves the lawyer owns the new address.</summary>
    private async Task<LawyerEmailVerifiedDto> ChangeEmailAsync(EmailVerificationToken token, LawyerProfile lawyer, string newEmail, CancellationToken cancellationToken)
    {
        const string changed = "EmailChanged";
        var user = lawyer.User!;
        if (token.UsedAtUtc is not null)
            return user.Email == newEmail
                ? new LawyerEmailVerifiedDto(lawyer.FullName, changed)
                : throw new InvalidOperationException("This activation link is invalid or has expired.");
        if (await db.Users.AnyAsync(u => u.Id != user.Id && u.Email == newEmail, cancellationToken))
            throw new InvalidOperationException("This email is already used by another account.");

        var oldEmail = user.Email;
        user.Email = newEmail;
        user.IsEmailVerified = true;
        token.UsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("LawyerEmailChanged", nameof(User), user.Id.ToString(),
            $"{oldEmail} -> {newEmail}", cancellationToken);
        return new LawyerEmailVerifiedDto(lawyer.FullName, changed);
    }
}

public record ResendLawyerVerificationEmailCommand : IRequest<Unit>;

public class ResendLawyerVerificationEmailHandler(
    ILawPortalDbContext db, ICurrentUser currentUser, ITokenService tokenService, IEmailSender emailSender, IConfiguration configuration)
    : IRequestHandler<ResendLawyerVerificationEmailCommand, Unit>
{
    public async Task<Unit> Handle(ResendLawyerVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var lawyer = await db.LawyerProfiles.Include(l => l.User).Include(l => l.License).FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);
        if (LawyerOnboarding.StepOf(lawyer) != OnboardingStep.VerifyEmail)
            throw new InvalidOperationException("There is no email waiting to be verified.");

        // ponytail: no per-account rate limit beyond the auth endpoints' global one; add one if abused.
        await LawyerOnboarding.SendVerificationEmailAsync(db, tokenService, emailSender, configuration, lawyer.User!, lawyer.FullName, cancellationToken);
        return Unit.Value;
    }
}

// ── Registration fee ─────────────────────────────────────────────────────────────────────────

public record RegistrationFeeDto(Guid InvoiceId, string Number, decimal BaseAmount, decimal VatAmount, decimal Total);

/// <summary>The lawyer's open fee invoice, created on first visit and repriced from the current
/// admin setting until it's paid.</summary>
public record OpenRegistrationFeeInvoiceCommand : IRequest<RegistrationFeeDto>;

public class OpenRegistrationFeeInvoiceHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<OpenRegistrationFeeInvoiceCommand, RegistrationFeeDto>
{
    public async Task<RegistrationFeeDto> Handle(OpenRegistrationFeeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await RegistrationFeeInvoices.OpenAsync(db, currentUser, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new RegistrationFeeDto(invoice.Id, invoice.Number, invoice.BaseAmount, invoice.VatAmount, invoice.Total);
    }
}

/// <summary>Card only, like subscriptions — lawyers have no wallet.</summary>
public record PayRegistrationFeeCommand(string? DiscountCode) : IRequest<CheckoutResultDto>;

public class PayRegistrationFeeHandler(ILawPortalDbContext db, ICurrentUser currentUser, IPaymentGateway gateway, IConfiguration configuration)
    : IRequestHandler<PayRegistrationFeeCommand, CheckoutResultDto>
{
    public async Task<CheckoutResultDto> Handle(PayRegistrationFeeCommand request, CancellationToken cancellationToken)
    {
        var invoice = await RegistrationFeeInvoices.OpenAsync(db, currentUser, cancellationToken);

        // A retry may bring a different code (or none): start again from the undiscounted price.
        await DiscountService.ReleaseAsync(db, invoice.Id, cancellationToken);
        var discount = string.IsNullOrWhiteSpace(request.DiscountCode)
            ? 0
            : (await DiscountService.ReserveAsync(db, request.DiscountCode, DiscountScope.LawyerRegistrationFee,
                currentUser.UserId!.Value, invoice.BaseAmount, invoice.Id, cancellationToken)).Amount;
        LawyerOnboarding.Price(invoice, invoice.BaseAmount, discount);

        var baseUrl = configuration["Payments:PublicBaseUrl"] ?? "http://localhost:5280";
        var result = await gateway.CreatePaymentAsync(
            invoice.Id, invoice.Total, "SAR", $"Law Portal — registration fee {invoice.Number}",
            $"{baseUrl}/api/v1/webhooks/payment-gateway", cancellationToken);

        invoice.GatewayProvider = gateway.Name;
        invoice.GatewayPaymentId = result.GatewayPaymentId;
        await db.SaveChangesAsync(cancellationToken);
        return new CheckoutResultDto(invoice.Id, invoice.Number, "Initiated", result.RedirectUrl, PaidImmediately: false);
    }
}

internal static class RegistrationFeeInvoices
{
    public static async Task<LawyerRegistrationFeeInvoice> OpenAsync(ILawPortalDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var lawyer = await db.LawyerProfiles.Include(l => l.User).Include(l => l.License).FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);
        if (LawyerOnboarding.StepOf(lawyer) != OnboardingStep.PayFee)
            throw new InvalidOperationException("No registration fee is due on this account.");

        var fee = await LawyerOnboarding.GetFeeSettingAsync(db, cancellationToken);
        var invoice = await db.RegistrationFeeInvoices
            .Where(i => i.LawyerProfileId == lawyerProfileId && i.Status != RegistrationFeeInvoiceStatus.Paid)
            .OrderByDescending(i => i.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (invoice is null)
        {
            var year = DateTime.UtcNow.Year;
            var countThisYear = await db.RegistrationFeeInvoices.IgnoreQueryFilters().CountAsync(i => i.CreatedAtUtc.Year == year, cancellationToken);
            invoice = new LawyerRegistrationFeeInvoice { Id = Guid.NewGuid(), LawyerProfileId = lawyerProfileId, Number = $"REG-{year}-{countThisYear + 1:D6}" };
            db.RegistrationFeeInvoices.Add(invoice);
        }

        // A failed attempt is simply tried again on the same invoice.
        invoice.Status = RegistrationFeeInvoiceStatus.Pending;
        invoice.FailureReason = null;
        LawyerOnboarding.Price(invoice, fee.Amount, invoice.DiscountAmount);
        return invoice;
    }
}

// ── Admin setting ────────────────────────────────────────────────────────────────────────────

public record RegistrationFeeSettingDto(decimal Amount, bool IsEnabled, decimal VatAmount, decimal Total);

public record GetRegistrationFeeSettingQuery : IRequest<RegistrationFeeSettingDto>;

public class GetRegistrationFeeSettingHandler(ILawPortalDbContext db) : IRequestHandler<GetRegistrationFeeSettingQuery, RegistrationFeeSettingDto>
{
    public async Task<RegistrationFeeSettingDto> Handle(GetRegistrationFeeSettingQuery request, CancellationToken cancellationToken) =>
        ToDto(await LawyerOnboarding.GetFeeSettingAsync(db, cancellationToken));

    internal static RegistrationFeeSettingDto ToDto(RegistrationFeeSetting s)
    {
        var vat = Math.Round(s.Amount * PaymentBreakdownCalculator.VatRate, 2);
        return new RegistrationFeeSettingDto(s.Amount, s.IsEnabled, vat, s.Amount + vat);
    }
}

public record UpdateRegistrationFeeSettingCommand(decimal Amount, bool IsEnabled) : IRequest<RegistrationFeeSettingDto>;

public class UpdateRegistrationFeeSettingValidator : AbstractValidator<UpdateRegistrationFeeSettingCommand>
{
    public UpdateRegistrationFeeSettingValidator() => RuleFor(x => x.Amount).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100_000);
}

public class UpdateRegistrationFeeSettingHandler(ILawPortalDbContext db, IAuditLogger auditLogger)
    : IRequestHandler<UpdateRegistrationFeeSettingCommand, RegistrationFeeSettingDto>
{
    public async Task<RegistrationFeeSettingDto> Handle(UpdateRegistrationFeeSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await db.RegistrationFeeSettings.FirstOrDefaultAsync(s => s.Id == RegistrationFeeSetting.SingletonId, cancellationToken);
        if (setting is null)
        {
            setting = new RegistrationFeeSetting { Id = RegistrationFeeSetting.SingletonId };
            db.RegistrationFeeSettings.Add(setting);
        }
        setting.Amount = request.Amount;
        setting.IsEnabled = request.IsEnabled;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        // Turning the fee off must not strand lawyers already waiting on the payment screen.
        if (!request.IsEnabled)
        {
            var waiting = await db.LawyerProfiles.Include(l => l.User).Include(l => l.License)
                .Where(l => l.User!.Status == UserStatus.PendingVerification && l.User.IsEmailVerified
                    && l.License!.VerificationStatus == LicenseVerificationStatus.Approved)
                .ToListAsync(cancellationToken);
            foreach (var lawyer in waiting) LawyerOnboarding.Activate(lawyer);
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("RegistrationFeeUpdated", nameof(RegistrationFeeSetting), setting.Id.ToString(), cancellationToken: cancellationToken);
        return GetRegistrationFeeSettingHandler.ToDto(setting);
    }
}
