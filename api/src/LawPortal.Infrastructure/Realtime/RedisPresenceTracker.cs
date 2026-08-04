using LawPortal.Application.Common.Interfaces;
using StackExchange.Redis;

namespace LawPortal.Infrastructure.Realtime;

/// <summary>
/// One Redis set per user (<c>presence:{userId}</c>), one member per open connection — a user
/// online in two tabs stays "online" until both connections close. First real consumer of the
/// Redis container that's sat unused in docker-compose since P0.
/// </summary>
public class RedisPresenceTracker(IConnectionMultiplexer redis) : IPresenceTracker
{
    private static string KeyFor(Guid userId) => $"presence:{userId}";

    public Task UserConnectedAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default) =>
        redis.GetDatabase().SetAddAsync(KeyFor(userId), connectionId);

    public async Task UserDisconnectedAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        await db.SetRemoveAsync(KeyFor(userId), connectionId);
    }

    public async Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await redis.GetDatabase().SetLengthAsync(KeyFor(userId)) > 0;
}
