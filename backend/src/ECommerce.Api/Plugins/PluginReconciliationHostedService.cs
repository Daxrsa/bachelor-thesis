using ECommerce.Core.Entities;
using ECommerce.Infrastructure;
using ECommerce.PluginRuntime;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Plugins;

public sealed class PluginReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    IPluginRuntime runtime,
    MarketplaceCatalog catalog,
    ILogger<PluginReconciliationHostedService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var installations = await db.PluginInstallations
            .OrderBy(plugin => plugin.PluginId)
            .ToListAsync(cancellationToken);

        var runningCount = 0;
        var failedCount = 0;

        foreach (var installation in installations)
        {
            try
            {
                var manifest = await catalog.GetAsync(installation.PluginId, cancellationToken);
                if (manifest is null)
                    throw new InvalidOperationException($"Plugin {installation.PluginId} is missing from the marketplace catalog");

                var running = await runtime.ReconcileAsync(manifest, cancellationToken);
                installation.Version = manifest.Version;
                installation.Image = manifest.Image;
                installation.ContainerName = running.InternalHost;
                installation.ContainerPort = running.Port;
                installation.State = PluginState.Running;
                installation.LastError = null;
                runningCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Failed to reconcile installed plugin {Plugin}", installation.PluginId);
                installation.State = PluginState.Failed;
                installation.LastError = ex.Message;
                failedCount++;
            }

            installation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        log.LogInformation(
            "Plugin reconciliation completed: {RunningCount} running, {FailedCount} failed",
            runningCount,
            failedCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
