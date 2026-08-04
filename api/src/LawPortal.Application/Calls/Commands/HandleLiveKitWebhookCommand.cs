using LawPortal.Domain.Calls;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Calls.Commands;

/// <summary>Records real call timing from LiveKit's own server-sent lifecycle events, rather
/// than trusting either client's local clock — the same "server is the source of truth"
/// principle as every other realtime feature in this project.</summary>
public record HandleLiveKitWebhookCommand(string EventType, string RoomName) : IRequest<Unit>;

public class HandleLiveKitWebhookHandler(ILawPortalDbContext db) : IRequestHandler<HandleLiveKitWebhookCommand, Unit>
{
    public async Task<Unit> Handle(HandleLiveKitWebhookCommand request, CancellationToken cancellationToken)
    {
        var session = await db.ConsultationSessions.FirstOrDefaultAsync(s => s.RoomName == request.RoomName, cancellationToken);
        if (session is null) return Unit.Value; // Not one of our rooms (or a stale/test event) — ignore.

        switch (request.EventType)
        {
            case "room_started":
                if (session.Status == CallSessionStatus.Scheduled)
                {
                    session.Status = CallSessionStatus.Live;
                    session.StartedAtUtc = DateTime.UtcNow;
                }
                break;

            case "room_finished":
                if (session.Status != CallSessionStatus.Ended)
                {
                    session.Status = CallSessionStatus.Ended;
                    session.EndedAtUtc = DateTime.UtcNow;
                    session.ActualDurationSeconds = session.StartedAtUtc is { } started
                        ? (int)session.EndedAtUtc!.Value.Subtract(started).TotalSeconds
                        : 0;
                }
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
