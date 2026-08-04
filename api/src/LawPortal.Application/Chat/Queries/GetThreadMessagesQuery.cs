using LawPortal.Application.Chat.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Chat.Queries;

/// <summary>
/// <paramref name="AfterUtc"/> is what makes reconnect-reconciliation possible: a client that
/// lost its socket mid-conversation re-fetches everything strictly after the last message it
/// actually has, rather than trusting the socket to have delivered everything in between.
/// </summary>
public record GetThreadMessagesQuery(Guid ThreadId, DateTime? AfterUtc, int Take = 100) : IRequest<IReadOnlyList<MessageDto>>;

public class GetThreadMessagesHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetThreadMessagesQuery, IReadOnlyList<MessageDto>>
{
    public async Task<IReadOnlyList<MessageDto>> Handle(GetThreadMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken)
            ?? throw new KeyNotFoundException("Thread not found.");
        if (thread.ClientUserId != userId && thread.LawyerUserId != userId)
            throw new UnauthorizedAccessException("You are not a participant in this thread.");

        var query = db.Messages.Where(m => m.ThreadId == request.ThreadId);
        if (request.AfterUtc is { } after)
            query = query.Where(m => m.SentAtUtc > after);

        return await query
            .OrderBy(m => m.SentAtUtc)
            .Take(request.Take)
            .Select(m => new MessageDto(m.Id, m.ThreadId, m.SenderUserId, m.Body, m.SentAtUtc))
            .ToListAsync(cancellationToken);
    }
}
