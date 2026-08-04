using LawPortal.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LawPortal.Infrastructure.HealthChecks;

/// <summary>A real connectivity probe (<c>SELECT 1</c>-equivalent via <c>CanConnectAsync</c>),
/// not just "the process is up" — the same distinction every dependency here draws between
/// liveness and readiness.</summary>
public class DatabaseHealthCheck(LawPortalDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("MySQL reachable.")
                : HealthCheckResult.Unhealthy("MySQL not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MySQL threw on connect.", ex);
        }
    }
}
