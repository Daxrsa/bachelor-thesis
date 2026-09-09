using ECommerce.Api.Plugins;
using ECommerce.PluginContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/marketplace")]
public sealed class MarketplaceController(
    IMarketplacePublishService marketplace,
    IPublisherRequestService publisherRequests) : ControllerBase
{
    [HttpGet("listings")]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await marketplace.ListListingsAsync(ct));

    [HttpGet("listings/mine")]
    [Authorize(Roles = "publisher,admin")]
    public async Task<IActionResult> ListMine(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return Ok(await marketplace.ListMineAsync(userId.Value, ct));
    }

    [HttpGet("listings/{pluginId}")]
    [Authorize(Roles = "publisher,admin")]
    public async Task<IActionResult> Get(string pluginId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return await ExecuteAsync(() => marketplace.GetOwnedAsync(pluginId, userId.Value, GetRole(), ct));
    }

    [HttpPost("listings")]
    [Authorize(Roles = "publisher,admin")]
    public async Task<IActionResult> Publish([FromBody] PluginManifest manifest, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return await ExecuteAsync(() => marketplace.PublishAsync(manifest, userId.Value, ct));
    }

    [HttpPut("listings/{pluginId}")]
    [Authorize(Roles = "publisher,admin")]
    public async Task<IActionResult> Update(string pluginId, [FromBody] PluginManifest manifest, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return await ExecuteAsync(() => marketplace.UpdateAsync(pluginId, manifest, userId.Value, ct));
    }

    [HttpDelete("listings/{pluginId}")]
    [Authorize(Roles = "publisher,admin")]
    public async Task<IActionResult> Delete(string pluginId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            await marketplace.DeleteAsync(pluginId, userId.Value, GetRole(), ct);
            return NoContent();
        }
        catch (MarketplaceException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }

    public sealed record PublisherRequestBody(string? Message);

    [HttpGet("publisher-requests/mine")]
    [Authorize]
    public async Task<IActionResult> GetMyPublisherRequest(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var mine = await publisherRequests.GetMineAsync(userId.Value, ct);
        return new JsonResult(mine);
    }

    [HttpPost("publisher-requests")]
    [Authorize]
    public async Task<IActionResult> RequestPublisher([FromBody] PublisherRequestBody? body, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return await ExecuteAsync(() => publisherRequests.RequestAsync(userId.Value, body?.Message, ct));
    }

    [HttpGet("publisher-requests")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ListPendingPublisherRequests(CancellationToken ct)
        => Ok(await publisherRequests.ListPendingAsync(ct));

    [HttpPost("publisher-requests/{id:guid}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ApprovePublisherRequest(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return await ExecuteAsync(() => publisherRequests.ApproveAsync(id, userId.Value, ct));
    }

    [HttpPost("publisher-requests/{id:guid}/reject")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RejectPublisherRequest(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return await ExecuteAsync(() => publisherRequests.RejectAsync(id, userId.Value, ct));
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> work)
    {
        try
        {
            return Ok(await work());
        }
        catch (MarketplaceException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string GetRole() =>
        User.FindFirst("role")?.Value
        ?? User.FindFirst(ClaimTypes.Role)?.Value
        ?? "user";
}
