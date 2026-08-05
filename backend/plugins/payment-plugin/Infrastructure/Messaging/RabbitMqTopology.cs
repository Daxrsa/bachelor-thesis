namespace PaymentPlugin.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string ExchangeName = "ecommerce.integration-events";
    public const string PaymentQueueName = "payment-plugin.integration-events";
}
