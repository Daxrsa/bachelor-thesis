using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/p/order-plugin/orders")]
[Authorize]
[Tags("Order Plugin")]
public sealed class OrderPluginController(IPluginService plugins, IHttpClientFactory http) : ControllerBase
{
    private const string PluginId = "order-plugin";

    [HttpGet]
    public Task ListOrders(CancellationToken ct) => ForwardAsync("orders", ct);

    [HttpGet("me")]
    public Task ListMyOrders(CancellationToken ct) => ForwardAsync("orders/me", ct);

    [HttpGet("greeting")]
    public Task GetGreeting(CancellationToken ct) => ForwardAsync("orders/greeting", ct);

    [HttpGet("me/{orderId}")]
    public Task GetMyOrder(string orderId, CancellationToken ct) => ForwardAsync($"orders/me/{orderId}", ct);

    [HttpPatch("{orderId}/status")]
    public Task UpdateOrderStatus(string orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct) =>
        ForwardAsync($"orders/{orderId}/status", HttpMethod.Patch, request, ct);

    private Task ForwardAsync(string path, CancellationToken ct) =>
        ForwardAsync(path, HttpMethod.Get, body: null, ct);

    private async Task ForwardAsync(string path, HttpMethod method, object? body, CancellationToken ct)
    {
        var resolved = await plugins.ResolveAsync(PluginId, ct);
        if (resolved is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { error = "Order plugin not installed" }, ct);
            return;
        }

        var (installation, _) = resolved.Value;
        if (installation.State != PluginState.Running)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { error = "Order plugin not running", state = installation.State.ToString() }, ct);
            return;
        }

        using var request = new HttpRequestMessage(method, plugins.BuildTarget(installation, path, Request.QueryString.Value ?? string.Empty));
        if (body is not null)
            request.Content = JsonContent.Create(body);

        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        request.Headers.TryAddWithoutValidation("X-User-Id", userId);
        request.Headers.TryAddWithoutValidation("X-User-Email", User.FindFirst("email")?.Value ?? string.Empty);

        var client = http.CreateClient("plugin-proxy");
        using var upstream = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        Response.StatusCode = (int)upstream.StatusCode;
        foreach (var header in upstream.Content.Headers)
            Response.Headers[header.Key] = header.Value.ToArray();
        Response.Headers.Remove("transfer-encoding");
        await upstream.Content.CopyToAsync(Response.Body, ct);
    }
}

public sealed record UpdateOrderStatusRequest(string Status);
