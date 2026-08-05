using System.Text;
using System.Text.Json;
using ECommerce.IntegrationContracts.V1;
using RabbitMQ.Client;

namespace CartPlugin.Infrastructure.Messaging;

public interface ICartEventPublisher
{
    Task PublishAsync<TPayload>(IntegrationEventEnvelope<TPayload> envelope, CancellationToken cancellationToken)
        where TPayload : class;
}

/// <summary>
/// Publishes cart integration events to the shared topic exchange instead of handling them in-process,
/// decoupling the HTTP call from the cart state update performed by <see cref="CartEventsConsumer"/>.
/// </summary>
public sealed class RabbitMqCartEventPublisher(RabbitMqConnectionProvider connectionProvider) : ICartEventPublisher
{
    public async Task PublishAsync<TPayload>(IntegrationEventEnvelope<TPayload> envelope, CancellationToken cancellationToken)
        where TPayload : class
    {
        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = envelope.EventId.ToString(),
            CorrelationId = envelope.CorrelationId
        };

        await channel.BasicPublishAsync(
            RabbitMqTopology.ExchangeName,
            envelope.EventName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
