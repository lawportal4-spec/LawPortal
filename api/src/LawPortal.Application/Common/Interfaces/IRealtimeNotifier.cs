using LawPortal.Application.Chat.Dtos;

namespace LawPortal.Application.Common.Interfaces;

/// <summary>
/// Pushes a cache-invalidation signal to a thread's connected participants — never a parallel
/// message store. REST is always the source of truth (see <c>SendMessageCommand</c>); this is
/// purely "go re-fetch," which is exactly what the plan's own architecture section specifies for
/// every realtime event in both web and mobile clients.
/// </summary>
public interface IRealtimeNotifier
{
    Task MessageSentAsync(Guid threadId, MessageDto message, CancellationToken cancellationToken = default);
}
