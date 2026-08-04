using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Files;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Attachments;

/// <summary>Registers an uploaded object against a draft request and scans it. Scanning
/// happens synchronously here — small enough files, and it keeps the "an EICAR file is
/// blocked" behaviour observable in a single request/response rather than needing to poll a
/// background job. A real production queue is a reasonable later refinement once file sizes
/// or scan volume make synchronous scanning too slow.</summary>
public record ConfirmAttachmentCommand(
    Guid RequestId,
    string StorageKey,
    string FileName,
    string ContentType) : IRequest<ConfirmedAttachmentDto>;

public class ConfirmAttachmentValidator : AbstractValidator<ConfirmAttachmentCommand>
{
    public ConfirmAttachmentValidator()
    {
        RuleFor(x => x.StorageKey).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).Must(AllowedContentTypes.IsAllowed)
            .WithMessage("Unsupported file type. Allowed: images, video, PDF, Word, Excel, archives.");
    }
}

public class ConfirmAttachmentHandler(ILawPortalDbContext db, ICurrentUser currentUser, IFileStorage storage, IVirusScanner scanner)
    : IRequestHandler<ConfirmAttachmentCommand, ConfirmedAttachmentDto>
{
    private const int MaxAttachmentsPerRequest = 5;

    public async Task<ConfirmedAttachmentDto> Handle(ConfirmAttachmentCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var draft = await db.ServiceRequests
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (draft.Status != RequestStatus.Draft)
            throw new InvalidOperationException("Cannot attach files to a submitted request.");

        if (draft.Attachments.Count >= MaxAttachmentsPerRequest)
            throw new InvalidOperationException($"A request can have at most {MaxAttachmentsPerRequest} attachments.");

        var sizeBytes = await storage.GetSizeAsync(request.StorageKey, cancellationToken);

        var attachment = new RequestAttachment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = draft.Id,
            StorageKey = request.StorageKey,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = sizeBytes,
            SortOrder = draft.Attachments.Count,
            ScanStatus = AttachmentScanStatus.Pending,
        };
        db.RequestAttachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);

        await using var content = await storage.OpenReadAsync(request.StorageKey, cancellationToken);
        var scanResult = await scanner.ScanAsync(content, cancellationToken);

        attachment.ScanStatus = scanResult.Outcome switch
        {
            ScanOutcome.Clean => AttachmentScanStatus.Clean,
            ScanOutcome.Infected => AttachmentScanStatus.Infected,
            _ => AttachmentScanStatus.ScanFailed,
        };
        attachment.ScanResult = scanResult.Detail;
        await db.SaveChangesAsync(cancellationToken);

        if (attachment.ScanStatus == AttachmentScanStatus.Infected)
            throw new InvalidOperationException("This file failed a security scan and was rejected.");

        return new ConfirmedAttachmentDto(attachment.Id, attachment.ScanStatus.ToString());
    }
}
