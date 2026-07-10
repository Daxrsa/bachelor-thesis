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
public sealed class PluginProxyController : ControllerBase
{
    private readonly IPluginService _svc;
    private readonly IHttpClientFactory _http;

    public PluginProxyController(IPluginService svc, IHttpClientFactory http)
    {
        _svc = svc;
        _http = http;
    }

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

        var target = $"http://{install.ContainerName}:{install.ContainerPort}/{path}{Request.QueryString}";
        var client = _http.CreateClient("plugin-proxy");

        using var forward = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        if (Request.ContentLength > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            forward.Content = new StreamContent(Request.Body);
            if (Request.ContentType is { Length: > 0 } ct1)
                forward.Content.Headers.TryAddWithoutValidation("Content-Type", ct1);
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
