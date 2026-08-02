namespace ECommerce.PluginContracts;

/// <summary>
/// Contract shipped by every plugin. Deserialized from plugin.json in the marketplace catalog
/// and stored alongside the plugin's install record.
/// </summary>
public sealed class PluginManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public required string Publisher { get; init; }

    /// <summary>Fully-qualified Docker image reference, e.g. "ghcr.io/acme/reviews-plugin:1.2.0".</summary>
    public required string Image { get; init; }

    /// <summary>Port the plugin's HTTP server listens on inside its container.</summary>
    public int ContainerPort { get; init; } = 8080;

    /// <summary>Relative URL the runtime hits to check readiness. Defaults to /health.</summary>
    public string HealthEndpoint { get; init; } = "/health";

    /// <summary>Semver range describing which host API this plugin targets, e.g. "^1.0.0".</summary>
    public string HostApi { get; init; } = "^1.0.0";

    /// <summary>Capability strings the plugin requests. The user must approve them at install time.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    /// <summary>Optional database sidecar owned by this plugin.</summary>
    public PluginDatabaseManifest? Database { get; init; }

    /// <summary>Optional UI extension points (slot => remote entry URL) surfaced to the Angular shell.</summary>
    public IReadOnlyDictionary<string, string> UiExtensions { get; init; } =
        new Dictionary<string, string>();
}

public sealed class PluginDatabaseManifest
{
    /// <summary>Database engine. Currently only "postgres" is supported by the Docker runtime.</summary>
    public string Engine { get; init; } = "postgres";

    /// <summary>Docker image used for the plugin-owned database sidecar.</summary>
    public string Image { get; init; } = "postgres:16-alpine";

    public string DatabaseName { get; init; } = "plugin";
    public string Username { get; init; } = "plugin";
    public string Password { get; init; } = "plugin";
    public int Port { get; init; } = 5432;
    public string VolumeMountPath { get; init; } = "/var/lib/postgresql/data";
}
