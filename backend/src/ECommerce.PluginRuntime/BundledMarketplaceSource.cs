using System.Text.Json;
using ECommerce.PluginContracts;
using Microsoft.Extensions.Logging;

namespace ECommerce.PluginRuntime;

/// <summary>File-backed official marketplace catalog (catalog.json).</summary>
public sealed class BundledMarketplaceSource : IMarketplaceSource
{
    private readonly string _catalogPath;
    private readonly ILogger<BundledMarketplaceSource>? _log;

    public BundledMarketplaceSource(
        string catalogPath,
        string name = "official",
        ILogger<BundledMarketplaceSource>? log = null)
    {
        _catalogPath = catalogPath;
        Name = name;
        _log = log;
    }

    public string Name { get; }

    public async Task<IReadOnlyList<PluginManifest>> ListAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_catalogPath))
        {
            _log?.LogWarning("Marketplace source {Source} path missing: {Path}", Name, _catalogPath);
            return Array.Empty<PluginManifest>();
        }

        await using var stream = File.OpenRead(_catalogPath);
        var items = await JsonSerializer.DeserializeAsync<List<PluginManifest>>(stream, JsonOpts, ct)
                    ?? new List<PluginManifest>();

        return items.Where(m => PluginIdRules.IsValid(m.Id)).ToList();
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
}
