using LawPortal.Application.Chat.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Chat.Queries;

public record GetThreadDetailQuery(Guid ThreadId) : IRequest<ThreadDetailDto>;

public class GetThreadDetailHandler(ILawPortalDbContext db, ICurrentUser currentUser, IPresenceTracker presence)
    : IRequestHandler<GetThreadDetailQuery, ThreadDetailDto>
{
    public async Task<ThreadDetailDto> Handle(GetThreadDetailQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken)
            ?? throw new KeyNotFoundException("Thread not found.");
        if (thread.ClientUserId != userId && thread.LawyerUserId != userId)
            throw new UnauthorizedAccessException("You are not a participant in this thread.");

        var serviceRequest = await db.ServiceRequests.FirstAsync(r => r.Id == thread.ServiceRequestId, cancellationToken);
        var otherPartyIsLawyer = thread.ClientUserId == userId;
        var otherPartyUserId = otherPartyIsLawyer ? thread.LawyerUserId : thread.ClientUserId;

        var otherPartyName = otherPartyIsLawyer
            ? await db.LawyerProfiles.Where(l => l.UserId == otherPartyUserId).Select(l => l.FullName).FirstOrDefaultAsync(cancellationToken)
            : await db.ClientProfiles.Where(c => c.UserId == otherPartyUserId).Select(c => c.FullName).FirstOrDefaultAsync(cancellationToken);

        var isBlockedByMe = await db.Blocks.AnyAsync(b => b.BlockerUserId == userId && b.BlockedUserId == otherPartyUserId, cancellationToken);
        var hasBlockedMe = await db.Blocks.AnyAsync(b => b.BlockerUserId == otherPartyUserId && b.BlockedUserId == userId, cancellationToken);
        var isOnline = await presence.IsOnlineAsync(otherPartyUserId, cancellationToken);

        return new ThreadDetailDto(
            thread.Id, thread.ServiceRequestId, serviceRequest.Number, otherPartyUserId,
            otherPartyName ?? "—", isOnline, isBlockedByMe, hasBlockedMe);
    }
}
