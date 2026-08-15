using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/p/files-plugin/files")]
[Authorize]
[Tags("Files Plugin")]
public sealed class FilesPluginController(IPluginService plugins, IHttpClientFactory http) : ControllerBase
{
    private const string PluginId = "files-plugin";

    private readonly IPluginService _plugins = plugins;
    private readonly IHttpClientFactory _http = http;

    [HttpGet("~/api/p/files-plugin/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task GetHealth(CancellationToken ct)
        => await ForwardAsync("health", ct);

    [HttpGet("greeting")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task GetGreeting(CancellationToken ct)
        => await ForwardAsync("files/greeting", ct);

    [HttpPost("images")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task UploadImage(CancellationToken ct)
        => await ForwardBodyAsync("files/images", HttpMethod.Post, ct);

    [HttpGet("images")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task ListImages(CancellationToken ct)
        => await ForwardAsync("files/images", ct);

    [HttpGet("images/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task GetImage(string fileName, CancellationToken ct)
        => await ForwardAsync($"files/images/{fileName}", ct);

    [HttpDelete("images/{fileName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task DeleteImage(string fileName, CancellationToken ct)
        => await ForwardAsync($"files/images/{fileName}", ct);

    private async Task ForwardAsync(string path, CancellationToken ct)
    {
        var target = await ResolveTargetAsync(path, ct);
        if (target is null)
            return;

        using var forward = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        await SendAsync(forward, ct);
    }

    private async Task ForwardBodyAsync(string path, HttpMethod method, CancellationToken ct)
    {
        var target = await ResolveTargetAsync(path, ct);
        if (target is null)
            return;

        using var forward = new HttpRequestMessage(method, target);

        if (Request.HasFormContentType)
        {
            // Rebuild the multipart body instead of piping Request.Body directly:
            // Kestrel has already parsed the incoming stream, so re-streaming it raw truncates the payload.
            var form = await Request.ReadFormAsync(ct);
            var multipart = new MultipartFormDataContent();

            foreach (var field in form)
                foreach (var value in field.Value)
                    multipart.Add(new StringContent(value ?? string.Empty), field.Key);

            foreach (var file in form.Files)
            {
                await using var stream = file.OpenReadStream();
                await using var memory = new MemoryStream();
                await stream.CopyToAsync(memory, ct);

                var fileContent = new ByteArrayContent(memory.ToArray());
                if (!string.IsNullOrWhiteSpace(file.ContentType))
                    fileContent.Headers.TryAddWithoutValidation("Content-Type", file.ContentType);

                multipart.Add(fileContent, file.Name, file.FileName);
            }

            forward.Content = multipart;
        }
        else
        {
            var buffer = new MemoryStream();
            await Request.Body.CopyToAsync(buffer, ct);
            forward.Content = new ByteArrayContent(buffer.ToArray());
            if (!string.IsNullOrWhiteSpace(Request.ContentType))
                forward.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
        }

        await SendAsync(forward, ct);
    }

    private async Task<string?> ResolveTargetAsync(string path, CancellationToken ct)
    {
        var resolved = await _plugins.ResolveAsync(PluginId, ct);
        if (resolved is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { error = "Plugin not installed" }, ct);
            return null;
        }

        var (install, _) = resolved.Value;
        if (install.State != PluginState.Running)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { error = "Plugin not running", state = install.State.ToString() }, ct);
            return null;
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
            // When this API runs in Docker, localhost points to this container itself.
            targetHost = "host.docker.internal";
        }

        return $"http://{targetHost}:{install.ContainerPort}/{path}{Request.QueryString}";
    }

    private async Task SendAsync(HttpRequestMessage forward, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            forward.Headers.TryAddWithoutValidation("X-User-Id", User.FindFirst("sub")?.Value ?? "");
            forward.Headers.TryAddWithoutValidation("X-User-Email", User.FindFirst("email")?.Value ?? "");
        }

        var client = _http.CreateClient("plugin-proxy");
        using var upstream = await client.SendAsync(forward, HttpCompletionOption.ResponseHeadersRead, ct);

        Response.StatusCode = (int)upstream.StatusCode;
        foreach (var header in upstream.Content.Headers)
            Response.Headers[header.Key] = header.Value.ToArray();
        Response.Headers.Remove("transfer-encoding");

        await upstream.Content.CopyToAsync(Response.Body, ct);
    }
}
