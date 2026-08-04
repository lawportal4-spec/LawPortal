using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Commands;

/// <summary>Step 3 of the consultation wizard — the title/details form. Voice-note duration is
/// validated here (5-minute cap); the recording itself is uploaded as a regular attachment.</summary>
public record UpdateConsultationDetailsCommand(
    Guid RequestId,
    string Title,
    string Description,
    DateTime? ScheduledStartUtc,
    string? VoiceNoteStorageKey,
    int? VoiceNoteDurationSeconds) : IRequest<Unit>;

public class UpdateConsultationDetailsValidator : AbstractValidator<UpdateConsultationDetailsCommand>
{
    public UpdateConsultationDetailsValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.VoiceNoteDurationSeconds).LessThanOrEqualTo(5 * 60)
            .When(x => x.VoiceNoteDurationSeconds.HasValue)
            .WithMessage("Voice notes are capped at 5 minutes.");
    }
}

public class UpdateConsultationDetailsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateConsultationDetailsCommand, Unit>
{
    public async Task<Unit> Handle(UpdateConsultationDetailsCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var draft = await db.ConsultationRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Draft not found.");

        if (draft.Status != RequestStatus.Draft)
            throw new InvalidOperationException("This request has already been submitted.");

        draft.Title = request.Title;
        draft.Description = request.Description;
        draft.ScheduledStartUtc = request.ScheduledStartUtc;
        draft.VoiceNoteStorageKey = request.VoiceNoteStorageKey;
        draft.VoiceNoteDurationSeconds = request.VoiceNoteDurationSeconds;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
