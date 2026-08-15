using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/p/cart-plugin/carts")]
[Authorize]
[Tags("Cart Plugin")]
public sealed class CartPluginController(IPluginService plugins, IHttpClientFactory http) : ControllerBase
{
    private const string PluginId = "cart-plugin";

    private readonly IPluginService _plugins = plugins;
    private readonly IHttpClientFactory _http = http;

    [HttpGet("me")]
    [EndpointSummary("Get the authenticated user's cart")]
    [EndpointDescription("Returns the current cart and all cart items for the authenticated user.")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task GetCart(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { error = "Authenticated user ID is missing" }, ct);
            return;
        }

        await ForwardAsync($"carts/{userId}", ct);
    }

    [HttpGet("me/items/count")]
    [EndpointSummary("Count the items in the authenticated user's cart")]
    [EndpointDescription("Returns the total number of units across all lines in the authenticated user's cart, or 0 when no cart exists.")]
    [ProducesResponseType(typeof(CartItemCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task GetItemCount(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { error = "Authenticated user ID is missing" }, ct);
            return;
        }

        await ForwardAsync($"carts/{userId}/items/count", ct);
    }

    [HttpDelete("me")]
    [EndpointSummary("Delete the authenticated user's cart")]
    [EndpointDescription("Deletes the entire cart for the authenticated user if it exists.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task DeleteMyCart(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { error = "Authenticated user ID is missing" }, ct);
            return;
        }

        await ForwardAsync($"carts/{userId}", ct);
    }

    [HttpPost("me/items")]
    [EndpointSummary("Add an item to the authenticated user's cart")]
    [EndpointDescription("Adds the requested product and quantity to the cart belonging to the authenticated user.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task AddItem([FromBody] AddProductToCartRequest request, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { error = "Authenticated user ID is missing" }, ct);
            return;
        }

        await ForwardJsonAsync($"carts/{userId}/items", HttpMethod.Post, request, ct);
    }

    [HttpDelete("me/items/{productId}")]
    [EndpointSummary("Remove an item from the authenticated user's cart")]
    [EndpointDescription("Removes the specified product from the cart belonging to the authenticated user.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task RemoveItem(string productId, [FromQuery] int quantity = 1, [FromQuery] string? cartId = null, [FromQuery] string? correlationId = null, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Quantity must be greater than 0" }, ct);
            return;
        }

        var userId = GetAuthenticatedUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { error = "Authenticated user ID is missing" }, ct);
            return;
        }

        var path = $"carts/{userId}/items/{productId}?quantity={quantity}";
        if (!string.IsNullOrWhiteSpace(cartId))
            path += $"&cartId={Uri.EscapeDataString(cartId)}";
        if (!string.IsNullOrWhiteSpace(correlationId))
            path += $"&correlationId={Uri.EscapeDataString(correlationId)}";

        await ForwardAsync(path, ct);
    }

    private async Task ForwardAsync(string path, CancellationToken ct)
    {
        var target = await ResolveTargetAsync(path, ct);
        if (target is null)
            return;

        using var forward = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        await SendAsync(forward, ct);
    }

    private async Task ForwardJsonAsync(string path, HttpMethod method, AddProductToCartRequest request, CancellationToken ct)
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
            forward.Headers.TryAddWithoutValidation("X-User-Id", GetAuthenticatedUserId() ?? "");
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

    private string? GetAuthenticatedUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}

public sealed record AddProductToCartRequest(
    string ProductId,
    int Quantity,
    string? CartId,
    string? CartItemId,
    string? CorrelationId);

public sealed record CartResponse(
    string Id,
    string UserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<CartItemResponse> Items);

public sealed record CartItemCountResponse(int Count);

public sealed record CartItemResponse(
    string Id,
    string ProductId,
    int Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
