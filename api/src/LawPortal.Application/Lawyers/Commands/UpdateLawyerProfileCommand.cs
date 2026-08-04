using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

public record UpdateLawyerProfileCommand(
    string? BioAr,
    string? BioEn,
    bool AcceptingNewRequests,
    IReadOnlyList<int> SpecialtyIds,
    IReadOnlyList<int> LanguageIds) : IRequest<Unit>;

public class UpdateLawyerProfileValidator : AbstractValidator<UpdateLawyerProfileCommand>
{
    public UpdateLawyerProfileValidator()
    {
        RuleFor(x => x.BioAr).MaximumLength(2000);
        RuleFor(x => x.BioEn).MaximumLength(2000);
        RuleFor(x => x.SpecialtyIds).NotEmpty().WithMessage("Select at least one specialty.");
    }
}

public class UpdateLawyerProfileHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateLawyerProfileCommand, Unit>
{
    public async Task<Unit> Handle(UpdateLawyerProfileCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var lawyer = await db.LawyerProfiles.FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);

        lawyer.BioAr = request.BioAr;
        lawyer.BioEn = request.BioEn;
        lawyer.AcceptingNewRequests = request.AcceptingNewRequests;

        var existingSpecialties = await db.LawyerSpecialties.Where(s => s.LawyerProfileId == lawyerProfileId).ToListAsync(cancellationToken);
        db.LawyerSpecialties.RemoveRange(existingSpecialties.Where(s => !request.SpecialtyIds.Contains(s.SpecialtyId)));
        foreach (var specialtyId in request.SpecialtyIds.Except(existingSpecialties.Select(s => s.SpecialtyId)))
            db.LawyerSpecialties.Add(new LawyerSpecialty { LawyerProfileId = lawyerProfileId, SpecialtyId = specialtyId });

        var existingLanguages = await db.LawyerLanguages.Where(l => l.LawyerProfileId == lawyerProfileId).ToListAsync(cancellationToken);
        db.LawyerLanguages.RemoveRange(existingLanguages.Where(l => !request.LanguageIds.Contains(l.LanguageId)));
        foreach (var languageId in request.LanguageIds.Except(existingLanguages.Select(l => l.LanguageId)))
            db.LawyerLanguages.Add(new LawyerLanguage { LawyerProfileId = lawyerProfileId, LanguageId = languageId });

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
