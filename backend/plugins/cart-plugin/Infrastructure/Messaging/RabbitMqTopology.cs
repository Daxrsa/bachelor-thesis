namespace CartPlugin.Infrastructure.Messaging;

/// <summary>Names shared by the cart plugin's publisher and consumer so they agree on the exchange/queue layout.</summary>
public static class RabbitMqTopology
{
    public const string ExchangeName = "ecommerce.integration-events";
    public const string CartQueueName = "cart-plugin.cart-events";
}
