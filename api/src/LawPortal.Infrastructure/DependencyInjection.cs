using LawPortal.Application.Common.Interfaces;
using LawPortal.Infrastructure.Identity;
using LawPortal.Infrastructure.Messaging;
using LawPortal.Infrastructure.Notifications;
using LawPortal.Infrastructure.Payments;
using LawPortal.Infrastructure.Persistence;
using LawPortal.Infrastructure.Realtime;
using LawPortal.Infrastructure.Storage;
using LawPortal.Infrastructure.Subscriptions;
using LawPortal.Infrastructure.VirusScanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LawPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' was not found.");

        services.AddDbContext<LawPortalDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySqlOptions => mySqlOptions.MigrationsAssembly(typeof(LawPortalDbContext).Assembly.FullName)));

        services.AddScoped<ILawPortalDbContext>(sp => sp.GetRequiredService<LawPortalDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IAuditLogger, EfAuditLogger>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IOtpSender, LoggingOtpSender>();
        if (!string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]))
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<IRecaptchaVerifier, NoOpRecaptchaVerifier>();
        services.AddSingleton<IFileStorage, S3FileStorage>();
        services.AddScoped<IVirusScanner, ClamAvVirusScanner>();

        // "Fake" (default) is the Development stand-in with no external account required — see
        // FakePaymentGateway's docs. Set Payments:Provider=Moyasar once real credentials exist.
        if (configuration["Payments:Provider"] == "Moyasar")
            services.AddSingleton<IPaymentGateway, MoyasarPaymentGateway>();
        else
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' was not found.");
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<IPresenceTracker, RedisPresenceTracker>();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
        services.AddSingleton<INotificationSender, LoggingNotificationSender>();
        services.AddSingleton<ILiveKitTokenService, LiveKitTokenService>();

        // The RabbitMQ container has sat unused in docker-compose since P0 — bid fan-out
        // (P9) is what finally consumes it. One shared, lazily-opened connection backs both
        // the publisher and the background consumer; see RabbitMqConnectionProvider's docs.
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IBidFanOutQueue, RabbitMqBidFanOutQueue>();
        services.AddHostedService<BidFanOutConsumer>();
        services.AddHostedService<SubscriptionRenewalService>();

        return services;
    }
}
