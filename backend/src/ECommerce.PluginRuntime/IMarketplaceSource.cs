using ECommerce.PluginContracts;

namespace ECommerce.PluginRuntime;

public interface IMarketplaceSource
{
    string Name { get; }
    Task<IReadOnlyList<PluginManifest>> ListAsync(CancellationToken ct = default);
}

public interface IMarketplaceCatalog
{
    Task<IReadOnlyList<PluginManifest>> ListAsync(CancellationToken ct = default);
    Task<PluginManifest?> GetAsync(string pluginId, CancellationToken ct = default);
}
