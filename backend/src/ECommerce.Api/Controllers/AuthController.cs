using ECommerce.Api.Auth;
using ECommerce.Api.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    private readonly IAuthService _auth = auth;

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        // JwtSecurityTokenHandler remaps the "sub" claim to ClaimTypes.NameIdentifier by default.
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await _auth.GetCurrentUserAsync(userId, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Credentials body, CancellationToken ct)
    {
        var res = await _auth.RegisterAsync(body.Email, body.Password, ct);
        return res.Success
            ? Ok(new { token = res.Token, expiresAt = res.ExpiresAt })
            : BadRequest(new { error = res.Error });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Credentials body, CancellationToken ct)
    {
        var res = await _auth.LoginAsync(body.Email, body.Password, ct);
        return res.Success
            ? Ok(new { token = res.Token, expiresAt = res.ExpiresAt })
            : Unauthorized(new { error = res.Error });
    }

    [HttpPost("dev-login")]
    public async Task<IActionResult> DevLogin(CancellationToken ct)
    {
        var res = await _auth.LoginAsync("daorsahyseni@gmail.com", "P@ssword123", ct);
        return Ok(res.Token);
    }

    [HttpPost("seed-user")]
    public async Task<IActionResult> SeedUser([FromBody] Credentials body, CancellationToken ct)
    {
        var res = await _auth.SeedUserAsync(body.Email, body.Password, ct);
        return Ok(new
        {
            seeded = res.Seeded,
            email = res.Email,
            message = res.Message
        });
    }

    public sealed record SetRoleRequest(string Email, string Role);
    public sealed record PromoteAdminRequest(string Email);

    /// <summary>Promote or demote a user role. Admin only.</summary>
    [Authorize(Roles = "admin")]
    [HttpPost("users/role")]
    public async Task<IActionResult> SetRole([FromBody] SetRoleRequest body, CancellationToken ct)
    {
        var res = await _auth.SetUserRoleAsync(body.Email, body.Role, ct);
        return res.Success
            ? Ok(new { email = res.Email, role = res.Role })
            : BadRequest(new { error = res.Error });
    }

    /// <summary>Development shortcut: grant admin without being signed in.</summary>
    [AllowAnonymous]
    [HttpPost("promote-admin")]
    public async Task<IActionResult> PromoteAdmin([FromBody] PromoteAdminRequest body, CancellationToken ct)
    {
        var res = await _auth.SetUserRoleAsync(body.Email, "admin", ct);
        return res.Success
            ? Ok(new { email = res.Email, role = res.Role })
            : BadRequest(new { error = res.Error });
    }
}
