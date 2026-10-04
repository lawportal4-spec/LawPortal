using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;

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

public class RequestClientOtpHandler(OtpService otpService, IRecaptchaVerifier recaptcha)
    : IRequestHandler<RequestClientOtpCommand, Unit>
{
    public async Task<Unit> Handle(RequestClientOtpCommand request, CancellationToken cancellationToken)
    {
        if (!await recaptcha.VerifyAsync(request.RecaptchaToken, cancellationToken))
            throw new ValidationException("reCAPTCHA verification failed.");

        await otpService.IssueAsync(request.PhoneE164, OtpPurpose.Login, cancellationToken);
        return Unit.Value;
    }
}
