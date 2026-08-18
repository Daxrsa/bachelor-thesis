using System.Text;
using System.Text.Json;
using ECommerce.IntegrationContracts.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPlugin.Application.Orders;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderPlugin.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "ecommerce";
    public string Password { get; set; } = "ecommerce";
    public string VirtualHost { get; set; } = "/";
}

public static class RabbitMqTopology
{
    public const string ExchangeName = "ecommerce.integration-events";
    public const string OrderQueueName = "order-plugin.integration-events";
}

public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true }) return _connection;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            _connection = await new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            }.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _lock.Dispose();
    }
}

public interface IOrderEventPublisher
{
    Task PublishAsync(IntegrationEventEnvelope<OrderCreatedEvent> envelope, CancellationToken cancellationToken);
}

public sealed class RabbitMqOrderEventPublisher(RabbitMqConnectionProvider connectionProvider) : IOrderEventPublisher
{
    public async Task PublishAsync(IntegrationEventEnvelope<OrderCreatedEvent> envelope, CancellationToken cancellationToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(RabbitMqTopology.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.BasicPublishAsync(
            RabbitMqTopology.ExchangeName,
            envelope.EventName,
            mandatory: false,
            basicProperties: new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = envelope.EventId.ToString(),
                CorrelationId = envelope.CorrelationId
            },
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope)),
            cancellationToken: cancellationToken);
    }
}

public sealed class OrderEventsConsumer(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderEventsConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync(RabbitMqTopology.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqTopology.OrderQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqTopology.OrderQueueName, RabbitMqTopology.ExchangeName, EventNames.PaymentSucceeded, cancellationToken: stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;
        await _channel.BasicConsumeAsync(RabbitMqTopology.OrderQueueName, autoAck: false, consumer, cancellationToken: stoppingToken);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            if (args.RoutingKey != EventNames.PaymentSucceeded)
                throw new InvalidOperationException($"Unexpected routing key {args.RoutingKey}");

            var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<PaymentSucceededEvent>>(
                Encoding.UTF8.GetString(args.Body.Span), JsonOptions)
                ?? throw new InvalidOperationException("Could not deserialize PaymentSucceeded envelope");
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
            await service.HandlePaymentSucceededAsync(envelope, CancellationToken.None);
            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order event with routing key {RoutingKey}", args.RoutingKey);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddOrderMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IOrderEventPublisher, RabbitMqOrderEventPublisher>();
        services.AddHostedService<OrderEventsConsumer>();
        return services;
    }
}
