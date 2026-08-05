using System.Text;
using System.Text.Json;
using ECommerce.IntegrationContracts.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentPlugin.Application.Payments;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentPlugin.Infrastructure.Messaging;

public sealed class PaymentEventsConsumer(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentEventsConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            RabbitMqTopology.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            RabbitMqTopology.PaymentQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(RabbitMqTopology.PaymentQueueName, RabbitMqTopology.ExchangeName, EventNames.ProductUpserted, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqTopology.PaymentQueueName, RabbitMqTopology.ExchangeName, EventNames.ProductDeleted, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqTopology.PaymentQueueName, RabbitMqTopology.ExchangeName, EventNames.CartCheckoutRequested, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            RabbitMqTopology.PaymentQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.Span);

            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            switch (args.RoutingKey)
            {
                case EventNames.ProductUpserted:
                {
                    var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<ProductUpsertedEvent>>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Could not deserialize ProductUpserted envelope");
                    await service.HandleProductUpsertedAsync(envelope, CancellationToken.None);
                    break;
                }
                case EventNames.ProductDeleted:
                {
                    var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<ProductDeletedEvent>>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Could not deserialize ProductDeleted envelope");
                    await service.HandleProductDeletedAsync(envelope, CancellationToken.None);
                    break;
                }
                case EventNames.CartCheckoutRequested:
                {
                    var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<CartCheckoutRequestedEvent>>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Could not deserialize CartCheckoutRequested envelope");
                    await service.HandleCartCheckoutRequestedAsync(envelope, CancellationToken.None);
                    break;
                }
                default:
                    logger.LogWarning("Ignoring unknown routing key {RoutingKey}", args.RoutingKey);
                    break;
            }

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process payment message with routing key {RoutingKey}", args.RoutingKey);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }
}
