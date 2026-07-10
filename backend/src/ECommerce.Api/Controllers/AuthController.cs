using ECommerce.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    public sealed record Credentials(string Email, string Password);

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
}
