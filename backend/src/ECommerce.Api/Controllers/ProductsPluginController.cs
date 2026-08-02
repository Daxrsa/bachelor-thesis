using System.Net.Http.Json;
using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/p/products-plugin/products")]
[Authorize]
[Tags("Products Plugin")]
public sealed class ProductsPluginController(IPluginService plugins, IHttpClientFactory http) : ControllerBase
{
    private const string PluginId = "products-plugin";

    private readonly IPluginService _plugins = plugins;
    private readonly IHttpClientFactory _http = http;

    [HttpGet]
    [ProducesResponseType(typeof(ProductResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task GetProducts([FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string[]? availability, CancellationToken ct)
        => await ForwardAsync("products", ct);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task GetProduct(string id, CancellationToken ct)
        => await ForwardAsync($"products/{id}", ct);

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task CreateProduct([FromBody] ProductRequest request, CancellationToken ct)
        => await ForwardJsonAsync("products", HttpMethod.Post, request, ct);

    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task UpdateProduct(string id, [FromBody] ProductRequest request, CancellationToken ct)
        => await ForwardJsonAsync($"products/{id}", HttpMethod.Put, request, ct);

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task DeleteProduct(string id, CancellationToken ct)
        => await ForwardAsync($"products/{id}", ct);

    private async Task ForwardAsync(string path, CancellationToken ct)
    {
        var target = await ResolveTargetAsync(path, ct);
        if (target is null)
            return;

        using var forward = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        await SendAsync(forward, ct);
    }

    private async Task ForwardJsonAsync(string path, HttpMethod method, ProductRequest request, CancellationToken ct)
    {
        var target = await ResolveTargetAsync(path, ct);
        if (target is null)
            return;

        using var forward = new HttpRequestMessage(method, target)
        {
            Content = JsonContent.Create(request)
        };
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

        return $"http://{install.ContainerName}:{install.ContainerPort}/{path}{Request.QueryString}";
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

public sealed record ProductRequest(
    string? Code,
    string Name,
    string? Description,
    decimal Price,
    int Quantity,
    string InventoryStatus,
    string? Category,
    string? Image,
    int Rating);

public sealed record ProductResponse(
    string Id,
    string? Code,
    string Name,
    string? Description,
    decimal Price,
    int Quantity,
    string InventoryStatus,
    string? Category,
    string? Image,
    int Rating);