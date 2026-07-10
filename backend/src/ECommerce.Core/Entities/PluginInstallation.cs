namespace ECommerce.Core.Entities;

public enum PluginState
{
    Installing,
    Running,
    Stopped,
    Failed
}

public sealed class PluginInstallation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Manifest identifier (unique per plugin, e.g. "reviews-plugin").</summary>
    public required string PluginId { get; set; }

    public required string Version { get; set; }
    public required string Image { get; set; }
    public required string ContainerName { get; set; }
    public int ContainerPort { get; set; }

    public PluginState State { get; set; } = PluginState.Installing;
    public string? LastError { get; set; }

    /// <summary>JSON-serialized permissions the user granted at install time.</summary>
    public string GrantedPermissionsJson { get; set; } = "[]";

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
