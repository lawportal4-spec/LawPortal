using LawPortal.Application.Chat.Dtos;
using LawPortal.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LawPortal.Infrastructure.Realtime;

public class SignalRRealtimeNotifier(IHubContext<ChatHub> hubContext) : IRealtimeNotifier
{
    public Task MessageSentAsync(Guid threadId, MessageDto message, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(ChatHub.GroupName(threadId)).SendAsync("MessageReceived", message, cancellationToken);
}
