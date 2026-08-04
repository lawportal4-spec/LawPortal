using LawPortal.Application.Calls.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Calls;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Calls.Commands;

/// <summary>Symmetric between client and lawyer, like chat — whichever side calls this for a
/// request they're a participant in gets a token for the same room.</summary>
public record IssueCallTokenCommand(Guid RequestId) : IRequest<CallTokenDto>;

public class IssueCallTokenHandler(ILawPortalDbContext db, ICurrentUser currentUser, ILiveKitTokenService tokenService, IConfiguration configuration)
    : IRequestHandler<IssueCallTokenCommand, CallTokenDto>
{
    public async Task<CallTokenDto> Handle(IssueCallTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var consultation = await db.ConsultationRequests.FirstOrDefaultAsync(c => c.Id == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        var clientUserId = await db.ClientProfiles.Where(c => c.Id == consultation.ClientId).Select(c => c.UserId).FirstAsync(cancellationToken);
        var lawyerUserId = await db.LawyerProfiles.Where(l => l.Id == consultation.LawyerProfileId).Select(l => l.UserId).FirstAsync(cancellationToken);
        if (userId != clientUserId && userId != lawyerUserId)
            throw new UnauthorizedAccessException("You are not a participant in this request.");

        if (consultation.Status != RequestStatus.InProgress)
            throw new InvalidOperationException("A call is only available once the lawyer has accepted the request.");

        if (consultation.ConsultationType == ConsultationType.Written)
            throw new InvalidOperationException("A written consultation has no call — use chat instead.");

        var session = await db.ConsultationSessions.FirstOrDefaultAsync(s => s.ServiceRequestId == consultation.Id, cancellationToken);
        if (session is null)
        {
            session = new ConsultationSession
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = consultation.Id,
                RoomName = $"request-{consultation.Id}",
                AllowedDurationSeconds = (consultation.SelectedDurationMinutes ?? 0) * 60,
            };
            db.ConsultationSessions.Add(session);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (session.Status == CallSessionStatus.Ended)
            throw new InvalidOperationException("This call has already ended.");

        // Once the call has actually started, a reconnecting participant only gets a token for
        // whatever time remains — this is what stops "just refresh and rejoin" from extending a
        // paid 15-minute slot into an unpaid 20-minute one.
        var elapsedSeconds = session.StartedAtUtc is { } startedAt
            ? (int)DateTime.UtcNow.Subtract(startedAt).TotalSeconds
            : 0;
        var remainingSeconds = Math.Max(30, session.AllowedDurationSeconds - elapsedSeconds);

        var liveKitSection = configuration.GetSection("LiveKit");
        var token = tokenService.CreateAccessToken(session.RoomName, userId.ToString(), TimeSpan.FromSeconds(remainingSeconds));

        return new CallTokenDto(
            liveKitSection["Url"] ?? "ws://localhost:7880",
            token,
            session.RoomName,
            session.AllowedDurationSeconds,
            session.StartedAtUtc is null ? null : elapsedSeconds);
    }
}
