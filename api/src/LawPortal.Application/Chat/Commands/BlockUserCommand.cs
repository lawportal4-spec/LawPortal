using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Chat.Commands;

public record BlockUserCommand(Guid ThreadId) : IRequest<Unit>;

public class BlockUserHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<BlockUserCommand, Unit>
{
    public async Task<Unit> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken)
            ?? throw new KeyNotFoundException("Thread not found.");
        if (thread.ClientUserId != userId && thread.LawyerUserId != userId)
            throw new UnauthorizedAccessException("You are not a participant in this thread.");

        var otherPartyUserId = thread.ClientUserId == userId ? thread.LawyerUserId : thread.ClientUserId;

        var alreadyBlocked = await db.Blocks.AnyAsync(
            b => b.BlockerUserId == userId && b.BlockedUserId == otherPartyUserId, cancellationToken);
        if (!alreadyBlocked)
        {
            db.Blocks.Add(new Block { Id = Guid.NewGuid(), BlockerUserId = userId, BlockedUserId = otherPartyUserId });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}

public record UnblockUserCommand(Guid ThreadId) : IRequest<Unit>;

public class UnblockUserHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<UnblockUserCommand, Unit>
{
    public async Task<Unit> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken)
            ?? throw new KeyNotFoundException("Thread not found.");
        if (thread.ClientUserId != userId && thread.LawyerUserId != userId)
            throw new UnauthorizedAccessException("You are not a participant in this thread.");

        var otherPartyUserId = thread.ClientUserId == userId ? thread.LawyerUserId : thread.ClientUserId;

        var block = await db.Blocks.FirstOrDefaultAsync(
            b => b.BlockerUserId == userId && b.BlockedUserId == otherPartyUserId, cancellationToken);
        if (block is not null)
        {
            db.Blocks.Remove(block);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
