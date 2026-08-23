using System.Text;
using System.Text.Json;
using CartPlugin.Application.Carts;
using ECommerce.IntegrationContracts.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CartPlugin.Infrastructure.Messaging;

/// <summary>
/// Consumes cart integration events published to the shared exchange and applies them to cart state,
/// providing the RabbitMQ wiring the plugin previously lacked.
/// </summary>
public sealed class CartEventsConsumer(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<CartEventsConsumer> logger) : BackgroundService
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
            RabbitMqTopology.CartQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(RabbitMqTopology.CartQueueName, RabbitMqTopology.ExchangeName, EventNames.ProductAddedToCart, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqTopology.CartQueueName, RabbitMqTopology.ExchangeName, EventNames.ProductRemovedFromCart, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqTopology.CartQueueName, RabbitMqTopology.ExchangeName, EventNames.PaymentSucceeded, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqTopology.CartQueueName, RabbitMqTopology.ExchangeName, EventNames.OrderCreated, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            RabbitMqTopology.CartQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.Span);

            switch (args.RoutingKey)
            {
                case EventNames.ProductAddedToCart:
                {
                    var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<ProductAddedToCartEvent>>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Could not deserialize ProductAddedToCartEvent envelope");
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                    await cartService.HandleProductAddedToCartEventAsync(envelope, CancellationToken.None);
                    break;
                }
                case EventNames.ProductRemovedFromCart:
                {
                    var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<ProductRemovedFromCartEvent>>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Could not deserialize ProductRemovedFromCartEvent envelope");
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                    await cartService.HandleProductRemovedFromCartEventAsync(envelope, CancellationToken.None);
                    break;
                }
                case EventNames.PaymentSucceeded:
                {
                    var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<PaymentSucceededEvent>>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Could not deserialize PaymentSucceededEvent envelope");
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                    await cartService.HandlePaymentSucceededAsync(envelope, CancellationToken.None);
                    break;
                }
                case EventNames.OrderCreated:
                {
                    var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<OrderCreatedEvent>>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Could not deserialize OrderCreatedEvent envelope");
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                    await cartService.HandleOrderCreatedAsync(envelope, CancellationToken.None);
                    break;
                }
                default:
                    logger.LogWarning("Ignoring message with unknown routing key {RoutingKey}", args.RoutingKey);
                    break;
            }

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message with routing key {RoutingKey}", args.RoutingKey);
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
