using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Forwards HTTP calls under /api/p/{pluginId}/... to the plugin's container.
/// Keeps plugins reachable only through the core, so auth, rate limits and audit are enforced centrally.
/// </summary>
[ApiController]
[Route("api/p")]
[Authorize]
public sealed class PluginProxyController(IPluginService svc, IHttpClientFactory http) : ControllerBase
{
    private readonly IPluginService _svc = svc;
    private readonly IHttpClientFactory _http = http;

    [AllowAnonymous]
    [HttpGet("files-plugin/static/images/{**path}")]
    public Task ProxyFilesPluginStatic(string? path, CancellationToken ct)
        => Proxy("files-plugin", $"static/images/{path}", ct);

    [Route("{pluginId}/{**path}")]
    [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch]
    public async Task Proxy(string pluginId, string? path, CancellationToken ct)
    {
        var resolved = await _svc.ResolveAsync(pluginId, ct);
        if (resolved is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { error = "Plugin not installed" }, ct);
            return;
        }

        var (install, _) = resolved.Value;
        if (install.State != PluginState.Running)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { error = "Plugin not running", state = install.State.ToString() }, ct);
            return;
        }

        var targetHost = install.ContainerName;
        var isRunningInContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (isRunningInContainer &&
            (string.Equals(targetHost, "localhost", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(targetHost, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(targetHost, "::1", StringComparison.OrdinalIgnoreCase)))
        {
            // Plugin installs created from a host-run API store localhost:<published-port>.
            // When proxying from inside Docker, localhost resolves to this API container itself.
            targetHost = "host.docker.internal";
        }

        var target = $"http://{targetHost}:{install.ContainerPort}/{path}{Request.QueryString}";
        var client = _http.CreateClient("plugin-proxy");

        using var forward = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        if (Request.ContentLength > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(ct);
                var multipart = new MultipartFormDataContent();

                foreach (var field in form)
                {
                    foreach (var value in field.Value)
                    {
                        multipart.Add(new StringContent(value), field.Key);
                    }
                }

                foreach (var file in form.Files)
                {
                    await using var stream = file.OpenReadStream();
                    await using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory, ct);

                    var fileContent = new ByteArrayContent(memory.ToArray());
                    if (!string.IsNullOrWhiteSpace(file.ContentType))
                    {
                        fileContent.Headers.TryAddWithoutValidation("Content-Type", file.ContentType);
                    }

                    multipart.Add(fileContent, file.Name, file.FileName);
                }

                forward.Content = multipart;
            }
            else
            {
                Request.EnableBuffering();
                if (Request.Body.CanSeek)
                    Request.Body.Position = 0;

                await using var buffer = new MemoryStream();
                await Request.Body.CopyToAsync(buffer, ct);

                if (Request.Body.CanSeek)
                    Request.Body.Position = 0;

                forward.Content = new ByteArrayContent(buffer.ToArray());
                if (Request.ContentType is { Length: > 0 } ct1)
                    forward.Content.Headers.TryAddWithoutValidation("Content-Type", ct1);
            }
        }

        // Forward the caller's identity so plugins can authorize per-user.
        if (User.Identity?.IsAuthenticated == true)
        {
            forward.Headers.TryAddWithoutValidation("X-User-Id", User.FindFirst("sub")?.Value ?? "");
            forward.Headers.TryAddWithoutValidation("X-User-Email", User.FindFirst("email")?.Value ?? "");
        }

        using var upstream = await client.SendAsync(forward, HttpCompletionOption.ResponseHeadersRead, ct);

        Response.StatusCode = (int)upstream.StatusCode;
        foreach (var h in upstream.Content.Headers)
            Response.Headers[h.Key] = h.Value.ToArray();
        Response.Headers.Remove("transfer-encoding");

        await upstream.Content.CopyToAsync(Response.Body, ct);
    }
}
