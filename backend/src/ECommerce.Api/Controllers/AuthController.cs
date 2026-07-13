using ECommerce.Api.Auth;
using ECommerce.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    private readonly IAuthService _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Credentials body, CancellationToken ct)
    {
        var res = await _auth.RegisterAsync(body.Email, body.Password, ct);
        return res.Success
            ? Ok(new { token = res.Token, expiresAt = res.ExpiresAt })
            : BadRequest(new { error = res.Error });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Credentials body, CancellationToken ct)
    {
        var res = await _auth.LoginAsync(body.Email, body.Password, ct);
        return res.Success
            ? Ok(new { token = res.Token, expiresAt = res.ExpiresAt })
            : Unauthorized(new { error = res.Error });
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
