using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Catalog;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Commands;

/// <summary>Step 1+2 of the documented wizard (specialty, then lawyer) collapse into one
/// draft-creation call — the frontend still renders them as two screens against this one draft.</summary>
public record CreateConsultationDraftCommand(
    int SpecialtyId,
    Guid LawyerProfileId,
    ConsultationType ConsultationType,
    int? DurationMinutes = null) : IRequest<Guid>;

public class CreateConsultationDraftValidator : AbstractValidator<CreateConsultationDraftCommand>
{
    private static readonly int[] ValidDurations = [15, 30, 45];

    public CreateConsultationDraftValidator()
    {
        RuleFor(x => x.SpecialtyId).GreaterThan(0);
        RuleFor(x => x.LawyerProfileId).NotEmpty();

        RuleFor(x => x.DurationMinutes)
            .Must(d => d is null)
            .When(x => x.ConsultationType == ConsultationType.Written)
            .WithMessage("A written consultation has no call duration to select.");

        RuleFor(x => x.DurationMinutes)
            .Must(d => d is not null && ValidDurations.Contains(d.Value))
            .When(x => x.ConsultationType != ConsultationType.Written)
            .WithMessage("Select a call duration of 15, 30, or 45 minutes.");
    }
}

public class CreateConsultationDraftHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateConsultationDraftCommand, Guid>
{
    public async Task<Guid> Handle(CreateConsultationDraftCommand request, CancellationToken cancellationToken)
    {
        var clientId = await ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var lawyerHasPricing = await db.LawyerProfiles
            .AnyAsync(l => l.Id == request.LawyerProfileId && l.IsVerified && l.Pricing != null, cancellationToken);
        if (!lawyerHasPricing)
            throw new InvalidOperationException("This lawyer is not currently bookable.");

        var slug = request.ConsultationType switch
        {
            ConsultationType.Instant => "instant-consultation",
            ConsultationType.Written => "written-consultation",
            ConsultationType.Scheduled => "scheduled-consultation",
            _ => throw new InvalidOperationException("Unknown consultation type."),
        };

        var consultationService = await db.ServiceCatalogItems
            .FirstOrDefaultAsync(s => s.Slug == slug && s.PricingModel == ServicePricingModel.PerLawyerFixed && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("No consultation service is configured.");

        var draft = new ConsultationRequest
        {
            Id = Guid.NewGuid(),
            Number = await RequestNumberGenerator.NextAsync(db, cancellationToken),
            ClientId = clientId,
            ServiceId = consultationService.Id,
            SpecialtyId = request.SpecialtyId,
            LawyerProfileId = request.LawyerProfileId,
            ConsultationType = request.ConsultationType,
            SelectedDurationMinutes = request.DurationMinutes,
        };

        db.ConsultationRequests.Add(draft);
        await db.SaveChangesAsync(cancellationToken);
        return draft.Id;
    }

    internal static async Task<Guid> ResolveClientProfileIdAsync(ILawPortalDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var clientId = await db.ClientProfiles.Where(c => c.UserId == userId).Select(c => c.Id).FirstOrDefaultAsync(cancellationToken);
        if (clientId == Guid.Empty) throw new UnauthorizedAccessException("No client profile for this account.");
        return clientId;
    }
}
