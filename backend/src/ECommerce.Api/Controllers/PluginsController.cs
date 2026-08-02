using ECommerce.Api.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/plugins")]
[Authorize]
public sealed class PluginsController(IPluginService svc) : ControllerBase
{
    private readonly IPluginService _svc = svc;

    public sealed record InstallRequest(IReadOnlyList<string> GrantedPermissions);

    /// <summary>Browse the marketplace catalog with an "installed" flag per entry.</summary>
    [HttpGet("marketplace")]
    [AllowAnonymous]
    public async Task<IActionResult> Marketplace(CancellationToken ct)
        => Ok(await _svc.ListMarketplaceAsync(ct));

    /// <summary>List plugins installed on this instance.</summary>
    [HttpGet]
    public async Task<IActionResult> Installed(CancellationToken ct)
        => Ok(await _svc.ListInstalledAsync(ct));

    [HttpPost("{pluginId}/install")]
    public async Task<IActionResult> Install(string pluginId, [FromBody] InstallRequest body, CancellationToken ct)
    {
        try
        {
            var record = await _svc.InstallAsync(pluginId, body.GrantedPermissions ?? Array.Empty<string>(), ct);
            return Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{pluginId}")]
    public async Task<IActionResult> Uninstall(string pluginId, CancellationToken ct)
    {
        await _svc.UninstallAsync(pluginId, ct);
        return NoContent();
    }
}
