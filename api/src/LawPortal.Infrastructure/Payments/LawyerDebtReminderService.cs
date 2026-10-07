using LawPortal.Application.Admin.LawyerDebts;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LawPortal.Infrastructure.Payments;

/// <summary>Emails every lawyer who owes the platform money, once per the configured interval
/// (refund policy setting), until it's paid — including lawyers who deleted their account.
/// Checks hourly; the interval itself is in days.</summary>
public class LawyerDebtReminderService(IServiceScopeFactory scopeFactory, ILogger<LawyerDebtReminderService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ILawPortalDbContext>();
                var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                var policy = await RefundPolicy.GetAsync(db, stoppingToken);
                var due = DateTime.UtcNow.AddDays(-policy.DebtReminderIntervalDays);

                var owing = await db.LawyerDebtEntries.GroupBy(e => e.LawyerProfileId)
                    .Where(g => g.Sum(e => e.Amount) > 0).Select(g => g.Key).ToListAsync(stoppingToken);
                var toRemind = await db.LawyerProfiles.IgnoreQueryFilters()
                    .Where(l => owing.Contains(l.Id) && (l.LastDebtReminderAtUtc == null || l.LastDebtReminderAtUtc < due))
                    .Select(l => l.Id).ToListAsync(stoppingToken);

                foreach (var id in toRemind)
                {
                    try { await LawyerDebtReminders.SendAsync(db, email, id, stoppingToken); }
                    catch (Exception ex) { logger.LogWarning(ex, "Debt reminder for lawyer {LawyerProfileId} failed.", id); }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lawyer debt reminder sweep failed; will retry next tick.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
