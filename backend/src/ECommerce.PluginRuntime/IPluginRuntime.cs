using ECommerce.PluginContracts;

namespace ECommerce.PluginRuntime;

/// <summary>Abstraction over the container runtime that hosts plugins.</summary>
public interface IPluginRuntime
{
    Task<RunningPlugin> StartAsync(PluginManifest manifest, CancellationToken ct = default);
    Task StopAsync(string pluginId, CancellationToken ct = default);
    Task<bool> IsRunningAsync(string pluginId, CancellationToken ct = default);
}

public sealed record RunningPlugin(string PluginId, string ContainerName, string InternalHost, int Port);
