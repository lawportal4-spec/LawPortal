using LawPortal.Application.Common.Interfaces;
using LawPortal.Infrastructure;
using LawPortal.Infrastructure.Persistence;
using LawPortal.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// A standalone migration runner, deliberately not the API process itself — a real deployment
// should never have "apply pending schema migrations" as a silent side effect of starting a web
// process (that's what api/src/LawPortal.Api/Program.cs does today, gated to Development only,
// which is fine for local dev but is not how this should work once a real environment exists).
//
// Builds a plain ServiceProvider from AddInfrastructure rather than a full generic Host on
// purpose: AddInfrastructure also registers BidFanOutConsumer/SubscriptionRenewalService as
// IHostedService, which only ever start if something calls IHost.StartAsync/RunAsync — a bare
// ServiceProvider never does that, so this process touches only what it explicitly resolves
// (the DbContext), and never needs Redis/RabbitMQ/MinIO reachable just to run a migration.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddInfrastructure(configuration);
await using var provider = services.BuildServiceProvider();

var db = provider.GetRequiredService<LawPortalDbContext>();

Console.WriteLine("Applying pending migrations...");
await db.Database.MigrateAsync();
Console.WriteLine("Migrations applied.");

if (args.Contains("--seed"))
{
    var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
    Console.WriteLine("Seeding reference/catalog data...");
    await CatalogSeeder.SeedAsync(db);
    await RbacSeeder.SeedAsync(db, passwordHasher);
    await ServiceCatalogSeeder.SeedAsync(db);
    await CommissionPolicySeeder.SeedAsync(db);
    await SubscriptionPlanSeeder.SeedAsync(db);
    Console.WriteLine("Reference/catalog seeding complete.");

    // Synthetic demo lawyers — real people are never generated, only production-shaped
    // pagination fixtures (see DevLawyerSeeder's own docs). Opt-in only: a real environment
    // should never get 550 fake lawyers by default.
    if (args.Contains("--with-demo-data"))
    {
        Console.WriteLine("Seeding synthetic demo lawyers (fixture data, not real people)...");
        await DevLawyerSeeder.SeedAsync(db);
        Console.WriteLine("Demo data seeding complete.");
    }
}

Console.WriteLine("Done.");
