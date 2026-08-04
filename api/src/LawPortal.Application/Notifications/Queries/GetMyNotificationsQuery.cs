using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Notifications.Queries;

public record GetMyNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;

public class GetMyNotificationsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        return await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Title, n.Body, n.RelatedThreadId, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
