using System.Text.Json;
using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LawPortal.Infrastructure.Messaging;

public record BidFanOutMessage(Guid BiddingRequestId);

/// <summary>
/// The publish side of "bid fan-out to ~1,000 lawyers goes through a queue, never a loop" — the
/// plan's own architecture rule. The HTTP request that submits a broadcast bidding request only
/// ever does this: publish one small message and return. All of the actual lawyer-matching and
/// bulk invitation-writing happens later, out of band, in <see cref="BidFanOutConsumer"/>.
/// </summary>
public class RabbitMqBidFanOutQueue(RabbitMqConnectionProvider connectionProvider, ILogger<RabbitMqBidFanOutQueue> logger)
    : IBidFanOutQueue
{
    public const string QueueName = "bidding.fanout";

    public async Task EnqueueAsync(Guid biddingRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(new BidFanOutMessage(biddingRequestId));
            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: QueueName, mandatory: false,
                basicProperties: properties, body: body, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Broadcast fan-out is background convenience, not a synchronous part of
            // submission — a lawyer who isn't reached this way isn't lost data, just delayed
            // visibility, and can be re-invited manually. Log loudly rather than fail the
            // client's already-successful submit call because the broker is briefly down.
            logger.LogError(ex, "Failed to enqueue bid fan-out for bidding request {BiddingRequestId}", biddingRequestId);
        }
    }
}
