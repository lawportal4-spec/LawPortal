using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Queries;

public record GetMeQuery : IRequest<MeDto>;

public class GetMeHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetMeQuery, MeDto>
{
    public async Task<MeDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var user = await db.Users
            .Include(u => u.ClientProfile)
            .Include(u => u.LawyerProfile!).ThenInclude(l => l.License)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        var profileCompleted = user.UserType switch
        {
            Domain.Identity.UserType.Client => user.ClientProfile?.FullName is not null,
            Domain.Identity.UserType.Lawyer => user.LawyerProfile is not null,
            _ => true,
        };

        return new MeDto(
            user.Id,
            user.UserType.ToString(),
            user.PhoneE164,
            user.Email,
            user.PreferredLocale,
            profileCompleted,
            currentUser.Roles,
            user.LawyerProfile?.License?.VerificationStatus.ToString(),
            user.ClientProfile?.HasAcceptedCurrentPledge ?? true);
    }
}
