using System.Text;
using ECommerce.Api.Auth;
using ECommerce.Api.Plugins;
using ECommerce.Infrastructure;
using ECommerce.PluginRuntime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing Jwt configuration");
builder.Services.AddSingleton(jwt);

builder.Services.Configure<PluginRuntimeOptions>(builder.Configuration.GetSection("PluginRuntime"));

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();

// --- Persistence ---
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- Auth ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// --- Plugin runtime + marketplace ---
builder.Services.AddSingleton<IPluginRuntime, DockerPluginRuntime>();
builder.Services.AddSingleton(sp =>
{
    var path = builder.Configuration["Marketplace:CatalogPath"]
               ?? throw new InvalidOperationException("Missing Marketplace:CatalogPath");
    return new MarketplaceCatalog(path);
});
builder.Services.AddScoped<IPluginService, PluginService>();
builder.Services.AddHttpClient("plugin-proxy");

// --- Web ---
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Startup: ensure schema (swap to db.Database.MigrateAsync() once you add EF migrations) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    // Native OpenAPI document at /openapi/v1.json
    app.MapOpenApi();
    // Scalar interactive API reference at /scalar/v1
    app.MapScalarApiReference(o => o
        .WithTitle("ECommerce Core API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
