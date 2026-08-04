using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LawPortal.Api;
using LawPortal.Api.Middleware;
using LawPortal.Application;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Infrastructure;
using LawPortal.Infrastructure.HealthChecks;
using LawPortal.Infrastructure.Persistence;
using LawPortal.Infrastructure.Persistence.Seeding;
using LawPortal.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

const string ClientOrigins = "ClientOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientOrigins, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173", // web-client
                "http://localhost:5174", // web-lawyer
                "http://localhost:5175", // web-admin
                "http://localhost:5176") // web-marketing (Astro) — 5176, not Astro's own 4321 default; see astro.config.mjs
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHttpClient(); // IHttpClientFactory — used by the dev fake-gateway's webhook self-call
builder.Services.AddSignalR();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Defense-in-depth alongside reCAPTCHA (still a dev-only no-op, see IRecaptchaVerifier) — a
// per-IP cap on auth endpoints so brute-forcing a password or OTP costs an attacker real wall
// time even before a real reCAPTCHA account exists.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 10,
            QueueLimit = 0,
        }));
});

// "ready" tags the checks that gate traffic (can this instance actually serve a request right
// now); /health/live intentionally runs none of them — a slow dependency should not make an
// orchestrator kill and restart an otherwise-healthy process.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured."))),
        };
        // A WebSocket upgrade can't carry a custom Authorization header, so SignalR's JS/every
        // client sends the token as ?access_token=... instead — the standard pattern for JWT + hubs.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Law Portal API — بوابة القانون",
        Version = "v1",
        Description = "REST API for بوابة القانون / Law Portal — a Saudi legal-services marketplace.",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            []
        },
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Law Portal API v1");
    });

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LawPortalDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.MigrateAsync();
    await CatalogSeeder.SeedAsync(db);
    await RbacSeeder.SeedAsync(db, passwordHasher);
    await ServiceCatalogSeeder.SeedAsync(db);
    await DevLawyerSeeder.SeedAsync(db); // synthetic demo data — Development only, see class docs
    await CommissionPolicySeeder.SeedAsync(db);
    await SubscriptionPlanSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseHttpMetrics(); // prometheus-net: per-request duration/count, scraped at /metrics below
app.UseCors(ClientOrigins);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapMetrics(); // self-hosted, Prometheus-format — no external observability vendor required
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "law-portal-api" }))
    .WithName("HealthCheck")
    .WithTags("Health");
// Liveness: is the process itself up — runs no dependency checks on purpose (see above).
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync });
// Readiness: can this instance actually serve a request right now (DB/Redis/RabbitMQ all reachable).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync,
});

app.Run();

public partial class Program; // exposed for LawPortal.Api.FunctionalTests' WebApplicationFactory
