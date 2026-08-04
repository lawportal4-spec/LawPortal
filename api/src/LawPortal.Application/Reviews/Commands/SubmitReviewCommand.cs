using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Requests;
using LawPortal.Domain.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Reviews.Commands;

public record SubmitReviewCommand(Guid RequestId, int Rating, string? Comment) : IRequest<Unit>;

public class SubmitReviewValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

/// <summary>The first real review for a given lawyer overwrites that lawyer's synthetic seeded
/// <c>AvgRating</c>/<c>RatingCount</c> (P2 seeded those only because no request system existed
/// yet to attach a real review to) — real data superseding demo data the moment it exists.</summary>
public class SubmitReviewHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<SubmitReviewCommand, Unit>
{
    public async Task<Unit> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var consultation = await db.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == request.RequestId && c.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (consultation.Status != RequestStatus.Completed)
            throw new InvalidOperationException("Only a completed request can be reviewed.");

        if (await db.Reviews.AnyAsync(r => r.ServiceRequestId == consultation.Id, cancellationToken))
            throw new InvalidOperationException("This request has already been reviewed.");

        db.Reviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = consultation.Id,
            ClientId = clientId,
            LawyerProfileId = consultation.LawyerProfileId,
            Rating = request.Rating,
            Comment = request.Comment,
        });

        var lawyer = await db.LawyerProfiles.FirstAsync(l => l.Id == consultation.LawyerProfileId, cancellationToken);
        var allRatings = await db.Reviews
            .Where(r => r.LawyerProfileId == consultation.LawyerProfileId)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);
        allRatings.Add(request.Rating); // this review isn't saved yet, so include it in-memory

        lawyer.RatingCount = allRatings.Count;
        lawyer.AvgRating = Math.Round((decimal)allRatings.Average(), 2);

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
