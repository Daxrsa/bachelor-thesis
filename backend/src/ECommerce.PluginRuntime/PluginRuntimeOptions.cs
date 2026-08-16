namespace ECommerce.PluginRuntime;

public sealed class PluginRuntimeOptions
{
    /// <summary>Docker daemon endpoint. On Linux/macOS this is unix:///var/run/docker.sock.</summary>
    public string DockerEndpoint { get; set; } = "unix:///var/run/docker.sock";

    /// <summary>Docker network plugins are attached to so the core can reach them by container name.</summary>
    public string Network { get; set; } = "ecommerce_plugins";

    /// <summary>Prefix used when naming plugin containers (e.g. "ecom-plugin-reviews").</summary>
    public string ContainerPrefix { get; set; } = "ecom-plugin-";

    /// <summary>How long to wait for the plugin's health endpoint before marking install failed.</summary>
    public TimeSpan HealthTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>RabbitMQ host reachable from plugin containers on the shared plugin network.</summary>
    public string BrokerHost { get; set; } = "rabbitmq";

    /// <summary>RabbitMQ AMQP port.</summary>
    public int BrokerPort { get; set; } = 5672;

    /// <summary>RabbitMQ username shared with plugins.</summary>
    public string BrokerUsername { get; set; } = "ecommerce";

    /// <summary>RabbitMQ password shared with plugins.</summary>
    public string BrokerPassword { get; set; } = "ecommerce";

    /// <summary>RabbitMQ virtual host used by plugin integration events.</summary>
    public string BrokerVirtualHost { get; set; } = "/";

    /// <summary>Stripe test/secret key forwarded to payment-plugin container as Stripe__SecretKey.</summary>
    public string StripeSecretKey { get; set; } = string.Empty;

    /// <summary>Stripe webhook signing secret forwarded to payment-plugin as STRIPE_WEBHOOK_SECRET.</summary>
    public string StripeWebhookSecret { get; set; } = string.Empty;
}
