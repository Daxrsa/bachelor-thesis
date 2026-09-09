namespace ECommerce.Core.Entities;

/// <summary>
/// A third-party plugin listing published into the remote marketplace (core Postgres).
/// Official first-party plugins remain in marketplace/catalog.json.
/// </summary>
public sealed class MarketplaceListing
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string PluginId { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public required string Description { get; set; }
    public required string Publisher { get; set; }
    public required string Image { get; set; }

    public int ContainerPort { get; set; } = 8080;
    public string HealthEndpoint { get; set; } = "/health";
    public string HostApi { get; set; } = "^1.0.0";

    public string PermissionsJson { get; set; } = "[]";
    public string? DatabaseJson { get; set; }
    public string? StorageJson { get; set; }
    public string UiExtensionsJson { get; set; } = "{}";

    public Guid PublishedByUserId { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
