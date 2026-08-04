using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LawPortal.Api;

/// <summary>The default health-check response is one bare status word — not enough to tell
/// which specific dependency (MySQL vs. Redis vs. RabbitMQ) is the one that's down without
/// SSH-ing in to check three different things by hand.</summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
            }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
