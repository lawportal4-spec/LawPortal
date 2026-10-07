using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

public record GetMyProfileForEditQuery : IRequest<LawyerProfileEditDto>;

public class GetMyProfileForEditHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyProfileForEditQuery, LawyerProfileEditDto>
{
    public async Task<LawyerProfileEditDto> Handle(GetMyProfileForEditQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var lawyer = await db.LawyerProfiles.Include(l => l.Pricing).FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);

        var specialtyIds = await db.LawyerSpecialties.Where(s => s.LawyerProfileId == lawyerProfileId).Select(s => s.SpecialtyId).ToListAsync(cancellationToken);
        var languageIds = await db.LawyerLanguages.Where(l => l.LawyerProfileId == lawyerProfileId).Select(l => l.LanguageId).ToListAsync(cancellationToken);

        var pricing = lawyer.Pricing is { } p
            ? new LawyerPricingEditDto(p.WrittenPrice, p.Price15, p.Price30, p.Price45)
            : null;
        return new LawyerProfileEditDto(lawyer.BioAr, lawyer.BioEn, lawyer.AcceptingNewRequests, specialtyIds, languageIds, pricing);
    }
}

public record GetLawyerReviewsQuery : IRequest<IReadOnlyList<LawyerReviewDto>>;

public class GetLawyerReviewsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetLawyerReviewsQuery, IReadOnlyList<LawyerReviewDto>>
{
    public async Task<IReadOnlyList<LawyerReviewDto>> Handle(GetLawyerReviewsQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        return await db.Reviews
            .Where(r => r.LawyerProfileId == lawyerProfileId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new LawyerReviewDto(r.Id, db.ServiceRequests.Where(sr => sr.Id == r.ServiceRequestId).Select(sr => sr.Number).First(), r.Rating, r.Comment, r.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
