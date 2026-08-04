using Serilog.Context;

namespace LawPortal.Api.Middleware;

/// <summary>Every log line for a request is tagged with the same id ASP.NET Core already
/// generates per-request (<see cref="HttpContext.TraceIdentifier"/>), and that id is echoed back
/// to the caller — so a client who reports "my request failed" can hand back one value that
/// greps the exact matching Serilog lines, instead of an operator reconstructing the request from
/// timestamp and path alone.</summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-Id"] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("RequestId", correlationId))
        {
            await next(context);
        }
    }
}
