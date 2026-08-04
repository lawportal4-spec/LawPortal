namespace LawPortal.Application.Common.Interfaces;

/// <summary>Backed by Redis (a set per user, one member per open connection) so presence survives
/// multiple tabs/devices and multiple API instances — the first real consumer of the Redis
/// container that's sat unused in docker-compose since P0.</summary>
public interface IPresenceTracker
{
    Task UserConnectedAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);
    Task UserDisconnectedAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);
    Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default);
}
