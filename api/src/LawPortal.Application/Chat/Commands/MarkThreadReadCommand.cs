using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Chat.Commands;

public record MarkThreadReadCommand(Guid ThreadId) : IRequest<Unit>;

public class MarkThreadReadHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<MarkThreadReadCommand, Unit>
{
    public async Task<Unit> Handle(MarkThreadReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken)
            ?? throw new KeyNotFoundException("Thread not found.");
        if (thread.ClientUserId != userId && thread.LawyerUserId != userId)
            throw new UnauthorizedAccessException("You are not a participant in this thread.");

        var participant = await db.ThreadParticipants
            .FirstOrDefaultAsync(p => p.ThreadId == thread.Id && p.UserId == userId, cancellationToken);
        if (participant is null)
        {
            participant = new ThreadParticipant { Id = Guid.NewGuid(), ThreadId = thread.Id, UserId = userId };
            db.ThreadParticipants.Add(participant);
        }
        participant.LastReadAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
