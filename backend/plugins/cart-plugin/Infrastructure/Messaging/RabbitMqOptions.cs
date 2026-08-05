namespace CartPlugin.Infrastructure.Messaging;

/// <summary>Bound from the RabbitMq__* environment variables injected by the plugin runtime.</summary>
public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "ecommerce";
    public string Password { get; set; } = "ecommerce";
    public string VirtualHost { get; set; } = "/";
}
