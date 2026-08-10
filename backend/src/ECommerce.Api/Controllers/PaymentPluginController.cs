using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/p/payment-plugin/payments")]
[Authorize]
[Tags("Payment Plugin")]
public sealed class PaymentPluginController(IPluginService plugins, IHttpClientFactory http) : ControllerBase
{
	private const string PluginId = "payment-plugin";
	private const string CartPluginId = "cart-plugin";

	private readonly IPluginService _plugins = plugins;
	private readonly IHttpClientFactory _http = http;

	[HttpGet("me")]
	[EndpointSummary("Get the authenticated user's payments")]
	[EndpointDescription("Returns the latest payment records for the authenticated user.")]
	[ProducesResponseType(typeof(PaymentResponse[]), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public async Task GetMyPayments(CancellationToken ct)
	{
		var userId = GetAuthenticatedUserId();
		if (string.IsNullOrWhiteSpace(userId))
		{
			Response.StatusCode = StatusCodes.Status401Unauthorized;
			await Response.WriteAsJsonAsync(new { error = "Authenticated user ID is missing" }, ct);
			return;
		}

		await ForwardAsync("payments/me", ct);
	}

	[HttpPost("me/pay")]
	[EndpointSummary("Pay for the authenticated user's cart")]
	[EndpointDescription("Initiates checkout for the authenticated user's cart. The final payment amount is computed by payment-plugin as sum(quantity * product price).")]
	[Consumes("application/json")]
	[ProducesResponseType(StatusCodes.Status202Accepted)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public async Task PayMyCart([FromBody] PayCartRequest request, CancellationToken ct)
	{
		var userId = GetAuthenticatedUserId();
		if (string.IsNullOrWhiteSpace(userId))
		{
			Response.StatusCode = StatusCodes.Status401Unauthorized;
			await Response.WriteAsJsonAsync(new { error = "Authenticated user ID is missing" }, ct);
			return;
		}

		if (string.IsNullOrWhiteSpace(request.CurrencyCode))
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			await Response.WriteAsJsonAsync(new { error = "CurrencyCode is required" }, ct);
			return;
		}

		if (string.IsNullOrWhiteSpace(request.PaymentMethod))
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			await Response.WriteAsJsonAsync(new { error = "PaymentMethod is required" }, ct);
			return;
		}

		var cartTarget = await ResolveTargetAsync(CartPluginId, "carts/me/checkout", ct);
		if (cartTarget is null)
			return;

		using var forward = new HttpRequestMessage(HttpMethod.Post, cartTarget)
		{
			Content = JsonContent.Create(new CheckoutCartRequest(
				request.CurrencyCode,
				request.PaymentMethod,
				request.CorrelationId))
		};

		await SendAsync(forward, ct);
	}

	private async Task ForwardAsync(string path, CancellationToken ct)
	{
		var target = await ResolveTargetAsync(PluginId, path, ct);
		if (target is null)
			return;

		using var forward = new HttpRequestMessage(new HttpMethod(Request.Method), target);
		await SendAsync(forward, ct);
	}

	private async Task<string?> ResolveTargetAsync(string pluginId, string path, CancellationToken ct)
	{
		var resolved = await _plugins.ResolveAsync(pluginId, ct);
		if (resolved is null)
		{
			Response.StatusCode = StatusCodes.Status404NotFound;
			await Response.WriteAsJsonAsync(new { error = $"Plugin '{pluginId}' not installed" }, ct);
			return null;
		}

		var (install, _) = resolved.Value;
		if (install.State != PluginState.Running)
		{
			Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
			await Response.WriteAsJsonAsync(new { error = $"Plugin '{pluginId}' not running", state = install.State.ToString() }, ct);
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

public sealed record PaymentResponse(
	string Id,
	string UserId,
	string CorrelationId,
	decimal Amount,
	string CurrencyCode,
	string Status,
	string PaymentMethod,
	string? ProviderReference,
	string? FailureReason,
	DateTimeOffset CreatedAtUtc);

public sealed record PayCartRequest(
	string CurrencyCode,
	string PaymentMethod,
	string? CorrelationId);

public sealed record CheckoutCartRequest(
	string CurrencyCode,
	string PaymentMethod,
	string? CorrelationId);
