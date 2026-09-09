using System.Text.Json;
using ECommerce.Infrastructure;
using ECommerce.PluginContracts;
using ECommerce.PluginRuntime;
using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Entities;

namespace ECommerce.Api.Plugins;

/// <summary>Reads third-party listings published into core Postgres.</summary>
public sealed class DatabaseMarketplaceSource(IServiceScopeFactory scopeFactory) : IMarketplaceSource
{
    public string Name => "database";

    public async Task<IReadOnlyList<PluginManifest>> ListAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var listings = await db.MarketplaceListings.AsNoTracking().ToListAsync(ct);
        return listings
            .Select(MarketplaceListingMapper.ToManifest)
            .Where(m => PluginIdRules.IsValid(m.Id))
            .ToList();
    }
}

public static class MarketplaceListingMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static PluginManifest ToManifest(MarketplaceListing listing)
    {
        IReadOnlyList<string> permissions = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(listing.PermissionsJson))
        {
            permissions = JsonSerializer.Deserialize<List<string>>(listing.PermissionsJson, JsonOpts)
                          ?? new List<string>();
        }

        PluginDatabaseManifest? database = null;
        if (!string.IsNullOrWhiteSpace(listing.DatabaseJson))
            database = JsonSerializer.Deserialize<PluginDatabaseManifest>(listing.DatabaseJson, JsonOpts);

        PluginStorageManifest? storage = null;
        if (!string.IsNullOrWhiteSpace(listing.StorageJson))
            storage = JsonSerializer.Deserialize<PluginStorageManifest>(listing.StorageJson, JsonOpts);

        IReadOnlyDictionary<string, string> uiExtensions = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(listing.UiExtensionsJson))
        {
            uiExtensions = JsonSerializer.Deserialize<Dictionary<string, string>>(listing.UiExtensionsJson, JsonOpts)
                           ?? new Dictionary<string, string>();
        }

        return new PluginManifest
        {
            Id = listing.PluginId,
            Name = listing.Name,
            Version = listing.Version,
            Description = listing.Description,
            Publisher = listing.Publisher,
            Image = listing.Image,
            ContainerPort = listing.ContainerPort,
            HealthEndpoint = listing.HealthEndpoint,
            HostApi = listing.HostApi,
            Permissions = permissions,
            Database = database,
            Storage = storage,
            UiExtensions = uiExtensions
        };
    }

    public static void ApplyManifest(MarketplaceListing listing, PluginManifest manifest)
    {
        listing.PluginId = manifest.Id;
        listing.Name = manifest.Name;
        listing.Version = manifest.Version;
        listing.Description = manifest.Description;
        listing.Publisher = manifest.Publisher;
        listing.Image = manifest.Image;
        listing.ContainerPort = manifest.ContainerPort;
        listing.HealthEndpoint = manifest.HealthEndpoint;
        listing.HostApi = manifest.HostApi;
        listing.PermissionsJson = JsonSerializer.Serialize(manifest.Permissions ?? Array.Empty<string>(), JsonOpts);
        listing.DatabaseJson = manifest.Database is null
            ? null
            : JsonSerializer.Serialize(manifest.Database, JsonOpts);
        listing.StorageJson = manifest.Storage is null
            ? null
            : JsonSerializer.Serialize(manifest.Storage, JsonOpts);
        listing.UiExtensionsJson = JsonSerializer.Serialize(
            manifest.UiExtensions ?? new Dictionary<string, string>(),
            JsonOpts);
        listing.UpdatedAt = DateTime.UtcNow;
    }
}
