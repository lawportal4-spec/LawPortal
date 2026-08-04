using LawPortal.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LawPortal.Infrastructure.HealthChecks;

/// <summary>RabbitMQ backs broadcast-bidding fan-out (P9) — the publisher already
/// swallows-and-logs a publish failure by design (fan-out is background convenience, not a
/// synchronous part of submission), which is exactly why this needs its own visible signal:
/// nothing else would ever surface a broker outage.</summary>
public class RabbitMqHealthCheck(RabbitMqConnectionProvider connectionProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ connection open.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection not open.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ not reachable.", ex);
        }
    }
}
