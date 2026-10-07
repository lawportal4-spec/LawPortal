using FluentValidation;
using LawPortal.Application.Auth.Commands;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

/// <summary>The lawyer's answer to "returned for correction": the licence step again, pre-filled.
/// A new document is optional unless the admin flagged the file itself. Puts the registration
/// back in the review queue; the admin's checklist is kept so the reviewer sees what was asked.</summary>
public record ResubmitLicenseCommand(
    LawyerLicenseType LicenseType,
    string LicenseNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    LicenseDocumentUpload? LicenseDocument) : IRequest<Unit>;

public class ResubmitLicenseValidator : AbstractValidator<ResubmitLicenseCommand>
{
    public ResubmitLicenseValidator()
    {
        RuleFor(x => x.LicenseType).IsInEnum();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpiryDate).GreaterThan(x => x.IssueDate);
        RuleFor(x => x.LicenseDocument!.Length).InclusiveBetween(1, RegisterLawyerValidator.MaxDocumentBytes)
            .WithMessage("The licence document must be at most 3MB.")
            .When(x => x.LicenseDocument is not null);
        RuleFor(x => x.LicenseDocument!.ContentType).Must(t => RegisterLawyerValidator.AllowedDocumentTypes.Contains(t))
            .WithMessage("The licence document must be a JPG or PNG image, or a PDF.")
            .When(x => x.LicenseDocument is not null);
    }
}

public class ResubmitLicenseHandler(
    ILawPortalDbContext db,
    ICurrentUser currentUser,
    IFileStorage storage,
    IVirusScanner scanner,
    IAuditLogger auditLogger)
    : IRequestHandler<ResubmitLicenseCommand, Unit>
{
    private const LicenseCorrectionIssue FileIssues = LicenseCorrectionIssue.FileUnreadable | LicenseCorrectionIssue.WrongFile;

    public async Task<Unit> Handle(ResubmitLicenseCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var license = await db.LawyerLicenses.FirstAsync(l => l.LawyerProfileId == lawyerProfileId, cancellationToken);

        if (license.VerificationStatus != LicenseVerificationStatus.ChangesRequested)
            throw new InvalidOperationException("This registration is not waiting for corrections.");

        if ((license.CorrectionIssues & FileIssues) != 0 && request.LicenseDocument is null)
            throw new ValidationException("Please upload a new licence document.");

        // Only what the admin flagged may change; a note with no checklist item leaves everything open.
        var issues = license.CorrectionIssues;
        bool Flagged(LicenseCorrectionIssue issue) => issues == LicenseCorrectionIssue.None || issues.HasFlag(issue);
        var canNumber = Flagged(LicenseCorrectionIssue.LicenseNumberMismatch);
        var canDates = Flagged(LicenseCorrectionIssue.DatesMismatch);
        var canType = Flagged(LicenseCorrectionIssue.LicenseTypeMismatch);
        var canFile = issues == LicenseCorrectionIssue.None || (issues & FileIssues) != 0;

        if (canNumber && await db.LawyerLicenses.AnyAsync(
                l => l.LicenseNumber == request.LicenseNumber && l.LawyerProfileId != lawyerProfileId, cancellationToken))
            throw new InvalidOperationException("This licence number is already registered.");

        if (canFile && request.LicenseDocument is { } document)
        {
            using var buffer = new MemoryStream();
            await document.Content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            if ((await scanner.ScanAsync(buffer, cancellationToken)).Outcome == ScanOutcome.Infected)
                throw new InvalidOperationException("This file failed a security scan and was rejected.");
            buffer.Position = 0;
            license.DocumentStorageKey = await storage.UploadAsync(Path.GetFileName(document.FileName), document.ContentType, buffer, cancellationToken);
            license.DocumentFileName = document.FileName;
            license.DocumentContentType = document.ContentType;
        }

        if (canType) license.LicenseType = request.LicenseType;
        if (canNumber) license.LicenseNumber = request.LicenseNumber;
        if (canDates)
        {
            license.IssueDate = request.IssueDate;
            license.ExpiryDate = request.ExpiryDate;
        }
        license.VerificationStatus = LicenseVerificationStatus.PendingReview;
        license.ResubmittedAtUtc = DateTime.UtcNow;

        await auditLogger.LogAsync("LawyerLicenseResubmitted", nameof(LawyerProfile), lawyerProfileId.ToString(), cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
