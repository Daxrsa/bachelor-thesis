using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Api.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<SeedUserResult> SeedUserAsync(string email, string password, CancellationToken ct = default);
}

public sealed record AuthResult(bool Success, string? Token, string? Error, DateTime? ExpiresAt);
public sealed record SeedUserResult(bool Seeded, string Email, string Message);

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;

    public AuthService(AppDbContext db, JwtOptions jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return new AuthResult(false, null, "Email already registered", null);

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return IssueToken(user);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return new AuthResult(false, null, "Invalid credentials", null);

        return IssueToken(user);
    }

    public async Task<SeedUserResult> SeedUserAsync(string email, string password, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var existingUser = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (existingUser)
            return new SeedUserResult(false, email, "User already exists");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return new SeedUserResult(true, email, "User created");
    }

    private AuthResult IssueToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResult(true, jwt, null, expires);
    }
}
