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
}
