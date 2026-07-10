using System.Text.Json;
using ECommerce.PluginContracts;

namespace ECommerce.PluginRuntime;

/// <summary>
/// Simple file-backed marketplace catalog. In production this would talk to a remote registry
/// (OCI catalog + a signed manifest index). For the thesis scaffold we read a JSON file.
/// </summary>
public sealed class MarketplaceCatalog
{
    private readonly string _catalogPath;

    public MarketplaceCatalog(string catalogPath) => _catalogPath = catalogPath;

    public async Task<IReadOnlyList<PluginManifest>> ListAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_catalogPath)) return Array.Empty<PluginManifest>();
        await using var s = File.OpenRead(_catalogPath);
        var items = await JsonSerializer.DeserializeAsync<List<PluginManifest>>(s, JsonOpts, ct);
        return items ?? new List<PluginManifest>();
    }

    public async Task<PluginManifest?> GetAsync(string pluginId, CancellationToken ct = default)
    {
        var all = await ListAsync(ct);
        return all.FirstOrDefault(p => p.Id == pluginId);
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
}
