using System.Text.Json;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Requests;
using LawPortal.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LawPortal.Infrastructure.Messaging;

/// <summary>
/// Consumes <see cref="BidFanOutMessage"/>s published by <see cref="RabbitMqBidFanOutQueue"/> and
/// does the actual work a synchronous HTTP request never should: matching potentially hundreds
/// of lawyers by specialty and bulk-inserting <see cref="RequestInvitation"/> rows. Idempotent
/// against redelivery — it only inserts lawyers not already invited, backed by the
/// (ServiceRequestId, LawyerProfileId) unique index as a second line of defense.
/// </summary>
public class BidFanOutConsumer(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<BidFanOutConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(RabbitMqBidFanOutQueue.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<BidFanOutMessage>(ea.Body.Span)
                            ?? throw new InvalidOperationException("Empty bid fan-out message.");

                        using var scope = scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ILawPortalDbContext>();
                        await FanOutAsync(db, message.BiddingRequestId, stoppingToken);

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process a bid fan-out message; requeuing for retry.");
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: RabbitMqBidFanOutQueue.QueueName,
                    autoAck: false,
                    consumerTag: string.Empty,
                    noLocal: false,
                    exclusive: false,
                    arguments: null!,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.LogInformation("Bid fan-out consumer connected and listening on '{Queue}'.", RabbitMqBidFanOutQueue.QueueName);

                while (!stoppingToken.IsCancellationRequested && channel.IsOpen)
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bid fan-out consumer lost its RabbitMQ connection; retrying in 5s.");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }
    }

    private static async Task FanOutAsync(ILawPortalDbContext db, Guid biddingRequestId, CancellationToken cancellationToken)
    {
        var bidding = await db.BiddingRequests.FirstOrDefaultAsync(b => b.Id == biddingRequestId, cancellationToken);
        // The request may have been cancelled (or, in theory, never committed) between publish
        // and delivery — nothing to fan out to in that case, not an error.
        if (bidding is null || bidding.SpecialtyId is not { } specialtyId) return;

        var alreadyInvited = await db.RequestInvitations
            .Where(i => i.ServiceRequestId == biddingRequestId)
            .Select(i => i.LawyerProfileId)
            .ToListAsync(cancellationToken);

        var matchingLawyerIds = await db.LawyerSpecialties
            .Where(ls => ls.SpecialtyId == specialtyId && !alreadyInvited.Contains(ls.LawyerProfileId))
            .Join(
                db.LawyerProfiles.Where(l => l.IsVerified && l.AcceptingNewRequests && l.Pricing != null),
                ls => ls.LawyerProfileId, l => l.Id, (_, l) => l.Id)
            .ToListAsync(cancellationToken);

        if (matchingLawyerIds.Count == 0) return;

        var eligibleLawyerIds = await FilterByBroadcastEntitlementAsync(db, matchingLawyerIds, cancellationToken);
        if (eligibleLawyerIds.Count == 0) return;

        foreach (var lawyerId in eligibleLawyerIds)
        {
            db.RequestInvitations.Add(new RequestInvitation
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = biddingRequestId,
                LawyerProfileId = lawyerId,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Broadcast bidding is a P10 subscription entitlement — a lawyer only gets matched
    /// this way while their effective plan includes it (Free-tier lawyers still take
    /// consultations and targeted bidding sends normally; they just don't get inbound broadcast
    /// leads for free). Deliberately bulk rather than calling
    /// <c>SubscriptionEntitlementResolver.GetEffectivePlanAsync</c> once per candidate — this
    /// consumer exists specifically so a broadcast to hundreds of lawyers never becomes a loop
    /// of per-lawyer queries.</summary>
    private static async Task<List<Guid>> FilterByBroadcastEntitlementAsync(
        ILawPortalDbContext db, List<Guid> candidateLawyerIds, CancellationToken cancellationToken)
    {
        var freePlanAllowsBroadcast = await db.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPrice)
            .Select(p => p.IncludesBroadcastBidding)
            .FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var overrides = await db.LawyerSubscriptions
            .Where(s => candidateLawyerIds.Contains(s.LawyerProfileId) && s.CurrentPeriodEndUtc > now
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Canceled))
            .Select(s => new { s.LawyerProfileId, s.Plan!.IncludesBroadcastBidding })
            .ToListAsync(cancellationToken);
        var overrideMap = overrides.ToDictionary(o => o.LawyerProfileId, o => o.IncludesBroadcastBidding);

        return candidateLawyerIds
            .Where(id => overrideMap.TryGetValue(id, out var allowed) ? allowed : freePlanAllowsBroadcast)
            .ToList();
    }
}
