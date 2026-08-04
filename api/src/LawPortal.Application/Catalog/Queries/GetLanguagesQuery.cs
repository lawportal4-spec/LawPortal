using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Catalog.Queries;

public record LanguageDto(int Id, string NameAr, string NameEn, string Code);

public record GetLanguagesQuery : IRequest<IReadOnlyList<LanguageDto>>;

public class GetLanguagesHandler(ILawPortalDbContext db) : IRequestHandler<GetLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    public async Task<IReadOnlyList<LanguageDto>> Handle(GetLanguagesQuery request, CancellationToken cancellationToken) =>
        await db.Languages
            .Where(l => l.IsActive)
            .Select(l => new LanguageDto(l.Id, l.NameAr, l.NameEn, l.Code))
            .ToListAsync(cancellationToken);
}
