using System.Text.Json;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure;
using ECommerce.PluginContracts;
using ECommerce.PluginRuntime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Plugins;

public interface IPluginService
{
    Task<IReadOnlyList<MarketplaceEntry>> ListMarketplaceAsync(CancellationToken ct);
    Task<IReadOnlyList<PluginInstallation>> ListInstalledAsync(CancellationToken ct);
    Task<PluginInstallation> InstallAsync(string pluginId, IReadOnlyList<string> grantedPermissions, CancellationToken ct);
    Task UninstallAsync(string pluginId, CancellationToken ct);
    Task<(PluginInstallation Install, PluginManifest Manifest)?> ResolveAsync(string pluginId, CancellationToken ct);
    string BuildTarget(PluginInstallation installation, string path, string queryString);
}

public sealed record MarketplaceEntry(PluginManifest Manifest, bool Installed);

public sealed class PluginService : IPluginService
{
    private readonly AppDbContext _db;
    private readonly MarketplaceCatalog _catalog;
    private readonly IPluginRuntime _runtime;
    private readonly PluginRuntimeOptions _opts;
    private readonly ILogger<PluginService> _log;

    public PluginService(
        AppDbContext db,
        MarketplaceCatalog catalog,
        IPluginRuntime runtime,
        IOptions<PluginRuntimeOptions> opts,
        ILogger<PluginService> log)
    {
        _db = db;
        _catalog = catalog;
        _runtime = runtime;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<MarketplaceEntry>> ListMarketplaceAsync(CancellationToken ct)
    {
        var catalog = await _catalog.ListAsync(ct);
        var installedIds = await _db.PluginInstallations.Select(p => p.PluginId).ToListAsync(ct);
        var set = installedIds.ToHashSet();
        return catalog.Select(m => new MarketplaceEntry(m, set.Contains(m.Id))).ToList();
    }

    public async Task<IReadOnlyList<PluginInstallation>> ListInstalledAsync(CancellationToken ct)
        => await _db.PluginInstallations.OrderBy(p => p.PluginId).ToListAsync(ct);

    public async Task<PluginInstallation> InstallAsync(string pluginId, IReadOnlyList<string> grantedPermissions, CancellationToken ct)
    {
        var manifest = await _catalog.GetAsync(pluginId, ct)
                       ?? throw new InvalidOperationException($"Plugin {pluginId} not in marketplace");

        var missing = manifest.Permissions.Except(grantedPermissions).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"Missing permission grants: {string.Join(", ", missing)}");

        var existing = await _db.PluginInstallations.FirstOrDefaultAsync(p => p.PluginId == pluginId, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Plugin {pluginId} already installed");

        var record = new PluginInstallation
        {
            PluginId = manifest.Id,
            Version = manifest.Version,
            Image = manifest.Image,
            ContainerName = _opts.ContainerPrefix + manifest.Id,
            ContainerPort = manifest.ContainerPort,
            State = PluginState.Installing,
            GrantedPermissionsJson = JsonSerializer.Serialize(grantedPermissions)
        };
        _db.PluginInstallations.Add(record);
        await _db.SaveChangesAsync(ct);

        try
        {
            var running = await _runtime.StartAsync(manifest, ct);
            record.ContainerName = running.InternalHost;
            record.ContainerPort = running.Port;
            record.State = PluginState.Running;
            record.LastError = null;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to start plugin {Plugin}", pluginId);
            record.State = PluginState.Failed;
            record.LastError = ex.Message;
        }

        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task UninstallAsync(string pluginId, CancellationToken ct)
    {
        var record = await _db.PluginInstallations.FirstOrDefaultAsync(p => p.PluginId == pluginId, ct);
        if (record is null) return;

        try { await _runtime.StopAsync(pluginId, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "Error stopping plugin {Plugin}", pluginId); }

        _db.PluginInstallations.Remove(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(PluginInstallation Install, PluginManifest Manifest)?> ResolveAsync(string pluginId, CancellationToken ct)
    {
        var install = await _db.PluginInstallations.FirstOrDefaultAsync(p => p.PluginId == pluginId, ct);
        if (install is null) return null;
        var manifest = await _catalog.GetAsync(pluginId, ct);
        if (manifest is null) return null;
        return (install, manifest);
    }

    public string BuildTarget(PluginInstallation installation, string path, string queryString)
        => $"http://{installation.ContainerName}:{installation.ContainerPort}/{path.TrimStart('/')}{queryString}";
}
