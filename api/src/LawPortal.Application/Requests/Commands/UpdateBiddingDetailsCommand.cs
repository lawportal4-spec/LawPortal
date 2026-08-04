using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Commands;

/// <summary>Step 3 of the bidding wizard — the title/details form, same shape as
/// <see cref="UpdateConsultationDetailsCommand"/>. Attachments are added via the shared
/// AttachmentsController and unlock only for the awarded lawyer — see BiddingRequest's docs.</summary>
public record UpdateBiddingDetailsCommand(Guid RequestId, string Title, string Description) : IRequest<Unit>;

public class UpdateBiddingDetailsValidator : AbstractValidator<UpdateBiddingDetailsCommand>
{
    public UpdateBiddingDetailsValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty();
    }
}

public class UpdateBiddingDetailsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateBiddingDetailsCommand, Unit>
{
    public async Task<Unit> Handle(UpdateBiddingDetailsCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var draft = await db.BiddingRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Draft not found.");

        if (draft.Status != RequestStatus.Draft)
            throw new InvalidOperationException("This request has already been submitted.");

        draft.Title = request.Title;
        draft.Description = request.Description;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
