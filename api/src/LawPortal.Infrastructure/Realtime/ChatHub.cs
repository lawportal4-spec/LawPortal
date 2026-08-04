using System.Security.Claims;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Infrastructure.Realtime;

/// <summary>
/// Transport only — connection lifecycle, presence, and group membership. Sending a message is
/// always a REST call (<c>POST /api/v1/chat/threads/{id}/messages</c>, via <c>SendMessageCommand</c>);
/// this hub never persists anything, matching the plan's own rule that a realtime event only
/// invalidates or patches the query cache. A hub method runs outside the normal HTTP pipeline, so
/// it reads the user id straight off <see cref="HubCallerContext.User"/> rather than through
/// <see cref="ICurrentUser"/> (which depends on <c>IHttpContextAccessor</c> and isn't reliably
/// populated for hub invocations).
/// </summary>
[Authorize]
public class ChatHub(LawPortalDbContext db, IPresenceTracker presence) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
            await presence.UserConnectedAsync(userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetUserId(out var userId))
            await presence.UserDisconnectedAsync(userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinThread(Guid threadId)
    {
        if (!TryGetUserId(out var userId)) return;

        var isParticipant = await db.MessageThreads.AnyAsync(t =>
            t.Id == threadId && (t.ClientUserId == userId || t.LawyerUserId == userId));
        if (!isParticipant) throw new HubException("You are not a participant in this thread.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(threadId));
    }

    public async Task LeaveThread(Guid threadId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(threadId));

    internal static string GroupName(Guid threadId) => $"thread-{threadId}";

    private bool TryGetUserId(out Guid userId)
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(sub, out userId);
    }
}
