using System.Text.Json;
using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure;
using ECommerce.PluginContracts;
using ECommerce.PluginRuntime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.Api.Tests;

public sealed class PluginReconciliationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_RefreshesEndpointsAndIsolatesFailures()
    {
        var catalogPath = Path.GetTempFileName();
        try
        {
            var manifests = new[]
            {
                CreateManifest("healthy-plugin"),
                CreateManifest("broken-plugin")
            };
            await File.WriteAllTextAsync(catalogPath, JsonSerializer.Serialize(manifests));

            var services = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            await using var provider = services.BuildServiceProvider();

            await using (var seedScope = provider.CreateAsyncScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.PluginInstallations.AddRange(
                    CreateInstallation("healthy-plugin", PluginState.Running),
                    CreateInstallation("broken-plugin", PluginState.Stopped));
                await db.SaveChangesAsync();
            }

            var runtime = new FakePluginRuntime();
            runtime.Results["healthy-plugin"] = new RunningPlugin(
                "healthy-plugin",
                "ecom-plugin-healthy-plugin",
                "localhost",
                49152);
            runtime.Errors["broken-plugin"] = new InvalidOperationException("health check failed");

            var service = new PluginReconciliationHostedService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                runtime,
                new CompositeMarketplaceCatalog(
                    new IMarketplaceSource[] { new BundledMarketplaceSource(catalogPath, name: "test") },
                    NullLogger<CompositeMarketplaceCatalog>.Instance),
                NullLogger<PluginReconciliationHostedService>.Instance);

            await service.StartAsync(CancellationToken.None);

            await using var assertScope = provider.CreateAsyncScope();
            var installations = await assertScope.ServiceProvider
                .GetRequiredService<AppDbContext>()
                .PluginInstallations
                .ToDictionaryAsync(plugin => plugin.PluginId);

            Assert.Equal(new[] { "broken-plugin", "healthy-plugin" }, runtime.ReconciledPluginIds);
            Assert.Equal(PluginState.Running, installations["healthy-plugin"].State);
            Assert.Equal("localhost", installations["healthy-plugin"].ContainerName);
            Assert.Equal(49152, installations["healthy-plugin"].ContainerPort);
            Assert.Null(installations["healthy-plugin"].LastError);
            Assert.Equal(PluginState.Failed, installations["broken-plugin"].State);
            Assert.Equal("health check failed", installations["broken-plugin"].LastError);
        }
        finally
        {
            File.Delete(catalogPath);
        }
    }

    private static PluginManifest CreateManifest(string pluginId) => new()
    {
        Id = pluginId,
        Name = pluginId,
        Version = "1.0.0",
        Description = "Test plugin",
        Publisher = "Tests",
        Image = $"example/{pluginId}:1.0.0"
    };

    private static PluginInstallation CreateInstallation(string pluginId, PluginState state) => new()
    {
        PluginId = pluginId,
        Version = "0.9.0",
        Image = $"example/{pluginId}:0.9.0",
        ContainerName = "stale-host",
        ContainerPort = 12345,
        State = state
    };

    private sealed class FakePluginRuntime : IPluginRuntime
    {
        public Dictionary<string, RunningPlugin> Results { get; } = [];
        public Dictionary<string, Exception> Errors { get; } = [];
        public List<string> ReconciledPluginIds { get; } = [];

        public Task<RunningPlugin> ReconcileAsync(PluginManifest manifest, CancellationToken ct = default)
        {
            ReconciledPluginIds.Add(manifest.Id);
            if (Errors.TryGetValue(manifest.Id, out var error))
                return Task.FromException<RunningPlugin>(error);

            return Task.FromResult(Results[manifest.Id]);
        }

        public Task<RunningPlugin> StartAsync(PluginManifest manifest, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task StopAsync(string pluginId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<bool> IsRunningAsync(string pluginId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
