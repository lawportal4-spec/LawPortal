using LawPortal.Application.Chat.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Chat.Queries;

public record GetMyThreadsQuery : IRequest<IReadOnlyList<ThreadSummaryDto>>;

public class GetMyThreadsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyThreadsQuery, IReadOnlyList<ThreadSummaryDto>>
{
    public async Task<IReadOnlyList<ThreadSummaryDto>> Handle(GetMyThreadsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var threads = await db.MessageThreads
            .Where(t => t.ClientUserId == userId || t.LawyerUserId == userId)
            .Select(t => new
            {
                t.Id,
                t.ServiceRequestId,
                t.ClientUserId,
                t.LawyerUserId,
                RequestNumber = t.ServiceRequest!.Number,
                LastMessage = t.Messages.OrderByDescending(m => m.SentAtUtc).FirstOrDefault(),
                MyLastReadAtUtc = t.Participants.Where(p => p.UserId == userId).Select(p => p.LastReadAtUtc).FirstOrDefault(),
                UnreadCount = t.Messages.Count(m => m.SenderUserId != userId
                    && (t.Participants.Where(p => p.UserId == userId).Select(p => p.LastReadAtUtc).FirstOrDefault() == null
                        || m.SentAtUtc > t.Participants.Where(p => p.UserId == userId).Select(p => p.LastReadAtUtc).FirstOrDefault()!.Value)),
            })
            .ToListAsync(cancellationToken);

        var result = new List<ThreadSummaryDto>();
        foreach (var t in threads)
        {
            var otherPartyIsLawyer = t.ClientUserId == userId;
            var otherPartyName = otherPartyIsLawyer
                ? await db.LawyerProfiles.Where(l => l.UserId == t.LawyerUserId).Select(l => l.FullName).FirstOrDefaultAsync(cancellationToken)
                : await db.ClientProfiles.Where(c => c.UserId == t.ClientUserId).Select(c => c.FullName).FirstOrDefaultAsync(cancellationToken);

            result.Add(new ThreadSummaryDto(
                t.Id, t.ServiceRequestId, t.RequestNumber, otherPartyName ?? "—",
                t.LastMessage?.Body, t.LastMessage?.SentAtUtc, t.UnreadCount));
        }

        return result.OrderByDescending(t => t.LastMessageAtUtc ?? DateTime.MinValue).ToList();
    }
}
