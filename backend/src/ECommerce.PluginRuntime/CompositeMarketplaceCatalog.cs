using ECommerce.PluginContracts;
using Microsoft.Extensions.Logging;

namespace ECommerce.PluginRuntime;

/// <summary>
/// Merges multiple marketplace sources. Earlier sources win on plugin-id collisions
/// (official/bundled should be registered first).
/// </summary>
public sealed class CompositeMarketplaceCatalog : IMarketplaceCatalog
{
    private readonly IReadOnlyList<IMarketplaceSource> _sources;
    private readonly ILogger<CompositeMarketplaceCatalog> _log;

    public CompositeMarketplaceCatalog(
        IEnumerable<IMarketplaceSource> sources,
        ILogger<CompositeMarketplaceCatalog> log)
    {
        _sources = sources.ToList();
        _log = log;
    }

    public async Task<IReadOnlyList<PluginManifest>> ListAsync(CancellationToken ct = default)
    {
        var byId = new Dictionary<string, PluginManifest>(StringComparer.Ordinal);

        foreach (var source in _sources)
        {
            IReadOnlyList<PluginManifest> items;
            try
            {
                items = await source.ListAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Marketplace source {Source} failed; continuing", source.Name);
                continue;
            }

            foreach (var manifest in items)
            {
                if (!PluginIdRules.IsValid(manifest.Id))
                {
                    _log.LogWarning(
                        "Skipping invalid plugin id {PluginId} from source {Source}",
                        manifest.Id, source.Name);
                    continue;
                }

                if (!byId.ContainsKey(manifest.Id))
                    byId[manifest.Id] = manifest;
                else
                    _log.LogDebug(
                        "Ignoring colliding plugin {PluginId} from {Source} (earlier source wins)",
                        manifest.Id, source.Name);
            }
        }

        return byId.Values.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
    }

    public async Task<PluginManifest?> GetAsync(string pluginId, CancellationToken ct = default)
    {
        var all = await ListAsync(ct);
        return all.FirstOrDefault(p => p.Id == pluginId);
    }
}
