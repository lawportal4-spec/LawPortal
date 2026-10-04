using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Lawyers;

/// <summary>Sends a registration back to the lawyer with what to fix — a checklist of
/// <see cref="LicenseCorrectionIssue"/> plus an optional note. The lawyer sees both on sign-in
/// and resubmits the licence step (<c>ResubmitLicenseCommand</c>).</summary>
public record RequestLawyerChangesCommand(Guid LawyerProfileId, IReadOnlyList<LicenseCorrectionIssue> Issues, string? Note)
    : IRequest<Unit>;

public class RequestLawyerChangesValidator : AbstractValidator<RequestLawyerChangesCommand>
{
    public RequestLawyerChangesValidator()
    {
        RuleFor(x => x).Must(x => x.Issues.Count > 0 || !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Tick at least one issue or write a note.");
        RuleForEach(x => x.Issues).IsInEnum().NotEqual(LicenseCorrectionIssue.None);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class RequestLawyerChangesHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<RequestLawyerChangesCommand, Unit>
{
    public async Task<Unit> Handle(RequestLawyerChangesCommand request, CancellationToken cancellationToken)
    {
        var license = await db.LawyerLicenses
            .FirstOrDefaultAsync(l => l.LawyerProfileId == request.LawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("No licence submitted for this lawyer.");

        if (license.VerificationStatus != LicenseVerificationStatus.PendingReview)
            throw new InvalidOperationException("Only a registration awaiting review can be returned for correction.");

        license.VerificationStatus = LicenseVerificationStatus.ChangesRequested;
        license.CorrectionIssues = request.Issues.Aggregate(LicenseCorrectionIssue.None, (all, i) => all | i);
        license.CorrectionNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        license.CorrectionRequestedAtUtc = DateTime.UtcNow;
        license.VerifiedByAdminUserId = currentUser.UserId;

        await auditLogger.LogAsync("LawyerLicenseChangesRequested", nameof(LawyerProfile), request.LawyerProfileId.ToString(),
            string.Join(", ", CorrectionIssueNames.Of(license.CorrectionIssues)), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
