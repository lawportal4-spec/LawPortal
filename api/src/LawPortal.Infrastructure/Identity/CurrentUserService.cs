using System.Security.Claims;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using Microsoft.AspNetCore.Http;

namespace LawPortal.Infrastructure.Identity;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public UserType? UserType
    {
        get
        {
            var value = Principal?.FindFirstValue("user_type");
            return Enum.TryParse<UserType>(value, out var type) ? type : null;
        }
    }

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
}
