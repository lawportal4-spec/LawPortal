using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace LawPortal.Infrastructure.HealthChecks;

/// <summary>Redis backs presence tracking (P5) — if it's down, online/offline status silently
/// goes stale rather than the app crashing, so this check exists to surface that degradation
/// instead of letting it hide.</summary>
public class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis reachable ({latency.TotalMilliseconds:F0}ms).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis not reachable.", ex);
        }
    }
}
