namespace LawPortal.Api.Middleware;

/// <summary>A baseline OWASP secure-headers set. This is defense-in-depth, not a substitute for
/// a real third-party penetration test — see the P13 progress log for what still needs one.</summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(self), microphone=(self), geolocation=()";

            // Swagger UI's bundled scripts/styles need 'unsafe-inline' and only run in
            // Development (see Program.cs) — a real API-only CSP applies everywhere else.
            headers["Content-Security-Policy"] = environment.IsDevelopment()
                ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'"
                : "default-src 'self'; frame-ancestors 'none'";
            return Task.CompletedTask;
        });

        await next(context);
    }
}
