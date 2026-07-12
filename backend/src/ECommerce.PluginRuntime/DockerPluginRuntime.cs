using System.Net.Http;
using Docker.DotNet;
using Docker.DotNet.Models;
using ECommerce.PluginContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.PluginRuntime;

/// <summary>
/// Talks to the local Docker daemon (via mounted socket) to pull, start, stop and health-check
/// plugin containers on a shared bridge network so the core API can reach them by name.
/// </summary>
public sealed class DockerPluginRuntime : IPluginRuntime, IDisposable
{
    private readonly DockerClient _docker;
    private readonly PluginRuntimeOptions _opts;
    private readonly ILogger<DockerPluginRuntime> _log;
    private readonly HttpClient _health = new();

    public DockerPluginRuntime(IOptions<PluginRuntimeOptions> opts, ILogger<DockerPluginRuntime> log)
    {
        _opts = opts.Value;
        _log = log;
        _docker = new DockerClientConfiguration(new Uri(_opts.DockerEndpoint)).CreateClient();
    }

    public async Task<RunningPlugin> StartAsync(PluginManifest manifest, CancellationToken ct = default)
    {
        var containerName = _opts.ContainerPrefix + manifest.Id;
        var internalPort = $"{manifest.ContainerPort}/tcp";
        var coreRunsInContainer = File.Exists("/.dockerenv");

        await EnsureNetworkAsync(ct);
        await EnsureImageAsync(manifest.Image, ct);
        await RemoveIfExistsAsync(containerName, ct);

        var hostConfig = new HostConfig
        {
            NetworkMode = _opts.Network,
            RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
        };

        // When the API runs on the host (dotnet watch), it cannot resolve container DNS names.
        // Publish a random host port so the API can call plugins via localhost.
        if (!coreRunsInContainer)
        {
            hostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                [internalPort] = new List<PortBinding> { new() { HostPort = "" } }
            };
        }

        var create = await _docker.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = manifest.Image,
            Name = containerName,
            Labels = new Dictionary<string, string>
            {
                ["ecommerce.plugin.id"] = manifest.Id,
                ["ecommerce.plugin.version"] = manifest.Version
            },
            HostConfig = hostConfig,
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                [internalPort] = default
            }
        }, ct);

        var started = await _docker.Containers.StartContainerAsync(create.ID, new ContainerStartParameters(), ct);
        if (!started)
            throw new InvalidOperationException($"Failed to start container for plugin {manifest.Id}");

        var resolved = await ResolveReachableEndpointAsync(create.ID, containerName, manifest.ContainerPort, coreRunsInContainer, ct);

        await WaitForHealthyAsync(resolved.Host, resolved.Port, manifest, ct);

        return new RunningPlugin(manifest.Id, containerName, resolved.Host, resolved.Port);
    }

    public async Task StopAsync(string pluginId, CancellationToken ct = default)
    {
        var name = _opts.ContainerPrefix + pluginId;
        await RemoveIfExistsAsync(name, ct);
    }

    public async Task<bool> IsRunningAsync(string pluginId, CancellationToken ct = default)
    {
        var name = _opts.ContainerPrefix + pluginId;
        var list = await _docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { [name] = true }
            }
        }, ct);
        return list.Any(c => string.Equals(c.State, "running", StringComparison.OrdinalIgnoreCase));
    }

    private async Task EnsureNetworkAsync(CancellationToken ct)
    {
        var nets = await _docker.Networks.ListNetworksAsync(new NetworksListParameters(), ct);
        if (nets.Any(n => n.Name == _opts.Network)) return;

        _log.LogInformation("Creating plugin network {Network}", _opts.Network);
        await _docker.Networks.CreateNetworkAsync(new NetworksCreateParameters
        {
            Name = _opts.Network,
            Driver = "bridge"
        }, ct);
    }

    private async Task EnsureImageAsync(string image, CancellationToken ct)
    {
        var (repo, tag) = SplitImage(image);
        _log.LogInformation("Pulling image {Image}", image);

        try
        {
            await _docker.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = repo, Tag = tag },
                authConfig: null,
                progress: new Progress<JSONMessage>(),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // For local development, allow manually built images even if registry pull fails.
            if (await ImageExistsLocallyAsync(image, ct))
            {
                _log.LogWarning(ex, "Pull failed for {Image}, using existing local image", image);
                return;
            }

            throw;
        }
    }

    private async Task<bool> ImageExistsLocallyAsync(string image, CancellationToken ct)
    {
        var list = await _docker.Images.ListImagesAsync(new ImagesListParameters { All = true }, ct);
        return list.Any(i => i.RepoTags?.Any(t => string.Equals(t, image, StringComparison.OrdinalIgnoreCase)) == true);
    }

    private static (string repo, string tag) SplitImage(string image)
    {
        var idx = image.LastIndexOf(':');
        // Handle registry ports like localhost:5000/foo — only split on the last colon after the last '/'.
        var slash = image.LastIndexOf('/');
        if (idx > slash && idx > 0)
            return (image[..idx], image[(idx + 1)..]);
        return (image, "latest");
    }

    private async Task RemoveIfExistsAsync(string containerName, CancellationToken ct)
    {
        var list = await _docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { [containerName] = true }
            }
        }, ct);

        foreach (var c in list)
        {
            _log.LogInformation("Removing existing container {Name} ({Id})", containerName, c.ID);
            await _docker.Containers.RemoveContainerAsync(c.ID, new ContainerRemoveParameters { Force = true }, ct);
        }
    }

    private async Task<(string Host, int Port)> ResolveReachableEndpointAsync(
        string containerId,
        string containerName,
        int containerPort,
        bool coreRunsInContainer,
        CancellationToken ct)
    {
        if (coreRunsInContainer)
            return (containerName, containerPort);

        var inspect = await _docker.Containers.InspectContainerAsync(containerId, ct);
        var key = $"{containerPort}/tcp";

        if (inspect.NetworkSettings?.Ports is not null &&
            inspect.NetworkSettings.Ports.TryGetValue(key, out var bindings) &&
            bindings is { Count: > 0 } &&
            int.TryParse(bindings[0].HostPort, out var hostPort))
        {
            return ("localhost", hostPort);
        }

        throw new InvalidOperationException($"Could not resolve published host port for plugin container {containerName}");
    }

    private async Task WaitForHealthyAsync(string host, int port, PluginManifest m, CancellationToken ct)
    {
        var url = $"http://{host}:{port}{m.HealthEndpoint}";
        var deadline = DateTime.UtcNow + _opts.HealthTimeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var res = await _health.GetAsync(url, ct);
                if (res.IsSuccessStatusCode) return;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Plugin {Plugin} not healthy yet", m.Id);
            }
            await Task.Delay(1000, ct);
        }
        throw new TimeoutException($"Plugin {m.Id} did not become healthy at {url} within {_opts.HealthTimeout.TotalSeconds}s");
    }

    public void Dispose()
    {
        _health.Dispose();
        _docker.Dispose();
    }
}
