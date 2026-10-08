using FluentValidation;
using LawPortal.Application.Auth.Commands;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Account;

// The "حسابي" page every account type has. Lawyers keep their richer page (LawyerAccount.cs) for
// photo, phone/email changes and location; this covers the name and password for everyone.

public record MyProfileDto(
    string UserType,
    string? Name,
    string? Email,
    string? PhoneE164,
    int? RegionId,
    int? CityId,
    bool HasPassword,
    bool CanEditName,
    DateTime MemberSinceUtc);

public record GetMyProfileQuery : IRequest<MyProfileDto>;

public class GetMyProfileHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetMyProfileQuery, MyProfileDto>
{
    public async Task<MyProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await MyAccount.LoadAsync(db, currentUser, cancellationToken);
        var cityId = user.ClientProfile?.CityId;
        int? regionId = cityId is null ? null
            : await db.Cities.Where(c => c.Id == cityId).Select(c => (int?)c.RegionId).FirstOrDefaultAsync(cancellationToken);

        return new MyProfileDto(
            user.UserType.ToString(),
            user.UserType switch
            {
                UserType.Client => user.ClientProfile?.FullName,
                UserType.Lawyer => user.LawyerProfile?.FullName,
                _ => user.AdminProfile?.DisplayName,
            },
            user.Email,
            user.PhoneE164,
            regionId,
            cityId,
            user.PasswordHash is not null,
            // A lawyer's name is the one on their licence; it changes only through re-registration.
            user.UserType != UserType.Lawyer,
            user.CreatedAtUtc);
    }
}

public record UpdateMyProfileCommand(string Name, int? CityId) : IRequest<Unit>;

public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
}

public class UpdateMyProfileHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<UpdateMyProfileCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await MyAccount.LoadAsync(db, currentUser, cancellationToken);
        var name = request.Name.Trim();

        switch (user.UserType)
        {
            case UserType.Client:
                if (request.CityId is not null && !await db.Cities.AnyAsync(c => c.Id == request.CityId, cancellationToken))
                    throw new InvalidOperationException("Unknown city.");
                user.ClientProfile ??= new ClientProfile { Id = Guid.NewGuid(), UserId = user.Id };
                user.ClientProfile.FullName = name;
                user.ClientProfile.CityId = request.CityId;
                user.ClientProfile.ProfileCompletedAtUtc ??= DateTime.UtcNow;
                break;
            case UserType.Admin:
                user.AdminProfile ??= new AdminProfile { Id = Guid.NewGuid(), UserId = user.Id, DisplayName = name };
                user.AdminProfile.DisplayName = name;
                break;
            default:
                throw new InvalidOperationException("A lawyer's name comes from their licence and can't be changed here.");
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("ProfileUpdated", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);
        return Unit.Value;
    }
}

public record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Unit>;

public class ChangeMyPasswordValidator : AbstractValidator<ChangeMyPasswordCommand>
{
    public ChangeMyPasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).LawyerPassword();
    }
}

public class ChangeMyPasswordHandler(ILawPortalDbContext db, ICurrentUser currentUser, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    : IRequestHandler<ChangeMyPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await MyAccount.LoadAsync(db, currentUser, cancellationToken);
        if (user.PasswordHash is null)
            throw new InvalidOperationException("This account signs in with a phone code and has no password.");
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("The current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        // ponytail: other devices keep their sessions until they expire; revoke all refresh tokens
        // here once the apps can re-issue one for the current session.
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("PasswordChanged", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);
        return Unit.Value;
    }
}

internal static class MyAccount
{
    public static async Task<User> LoadAsync(ILawPortalDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return await db.Users
                   .Include(u => u.ClientProfile)
                   .Include(u => u.LawyerProfile)
                   .Include(u => u.AdminProfile)
                   .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
               ?? throw new UnauthorizedAccessException();
    }
}
