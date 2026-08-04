using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace LawPortal.Infrastructure.Messaging;

/// <summary>
/// Lazily opens (and reopens) the one shared RabbitMQ connection, rather than connecting at
/// application startup — the container is real (see docker-compose's <c>rabbitmq</c> service,
/// unused since P0), but a lawyer submitting a broadcast bidding request should never fail
/// because the broker happens to be briefly unreachable. Both the publisher
/// (<see cref="RabbitMqBidFanOutQueue"/>) and the consumer (<see cref="BidFanOutConsumer"/>)
/// go through this instead of holding their own connection.
/// </summary>
public class RabbitMqConnectionProvider(IConfiguration configuration)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;

            var connectionString = configuration.GetConnectionString("RabbitMq")
                ?? throw new InvalidOperationException("Connection string 'RabbitMq' was not found.");
            var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }
}
