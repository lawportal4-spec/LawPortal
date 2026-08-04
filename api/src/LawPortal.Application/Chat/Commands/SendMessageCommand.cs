using FluentValidation;
using LawPortal.Application.Chat.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Chat;
using LawPortal.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Chat.Commands;

public record SendMessageCommand(Guid ThreadId, string Body) : IRequest<MessageDto>;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
}

public class SendMessageHandler(
    ILawPortalDbContext db,
    ICurrentUser currentUser,
    IPresenceTracker presence,
    INotificationSender notificationSender,
    IRealtimeNotifier realtime)
    : IRequestHandler<SendMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken)
            ?? throw new KeyNotFoundException("Thread not found.");

        if (thread.ClientUserId != userId && thread.LawyerUserId != userId)
            throw new UnauthorizedAccessException("You are not a participant in this thread.");

        var otherPartyUserId = thread.ClientUserId == userId ? thread.LawyerUserId : thread.ClientUserId;

        var isBlocked = await db.Blocks.AnyAsync(
            b => (b.BlockerUserId == userId && b.BlockedUserId == otherPartyUserId)
                || (b.BlockerUserId == otherPartyUserId && b.BlockedUserId == userId),
            cancellationToken);
        if (isBlocked)
            throw new InvalidOperationException("You can't message this participant.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ThreadId = thread.Id,
            SenderUserId = userId,
            Body = request.Body,
        };
        db.Messages.Add(message);

        var senderParticipant = await db.ThreadParticipants
            .FirstOrDefaultAsync(p => p.ThreadId == thread.Id && p.UserId == userId, cancellationToken);
        if (senderParticipant is null)
        {
            senderParticipant = new ThreadParticipant { Id = Guid.NewGuid(), ThreadId = thread.Id, UserId = userId };
            db.ThreadParticipants.Add(senderParticipant);
        }
        senderParticipant.LastReadAtUtc = message.SentAtUtc;

        if (!await presence.IsOnlineAsync(otherPartyUserId, cancellationToken))
        {
            var preview = request.Body.Length > 120 ? request.Body[..120] + "…" : request.Body;
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = otherPartyUserId,
                Type = NotificationType.NewMessage,
                Title = "New message",
                Body = preview,
                RelatedThreadId = thread.Id,
            });
            await notificationSender.SendAsync(otherPartyUserId, "New message", preview, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var dto = new MessageDto(message.Id, message.ThreadId, message.SenderUserId, message.Body, message.SentAtUtc);
        await realtime.MessageSentAsync(thread.Id, dto, cancellationToken);
        return dto;
    }
}
