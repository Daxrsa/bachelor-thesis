using System.Text;
using ECommerce.Api.Auth;
using ECommerce.Api.Plugins;
using ECommerce.Infrastructure;
using ECommerce.PluginRuntime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Microsoft.OpenApi;

LoadDotEnvIfPresent();

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing Jwt configuration");
builder.Services.AddSingleton(jwt);
builder.Services.AddSwaggerGen(options =>
{
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the JWT access token.",
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

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
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the JWT access token."
    };

    document.Security ??= [];
    document.Security.Add(new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });

    return Task.CompletedTask;
}));

var app = builder.Build();

var staticFilesRoot = ResolvePath(
    app.Environment.ContentRootPath,
    builder.Configuration["StaticFiles:RootPath"] ?? Path.Combine("storage", "public"));
Directory.CreateDirectory(staticFilesRoot);

var staticFilesRequestPath = builder.Configuration["StaticFiles:RequestPath"] ?? "/static";

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

    app.UseSwagger();   
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(staticFilesRoot),
    RequestPath = staticFilesRequestPath
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static void LoadDotEnvIfPresent()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env")
    };

    var envPath = candidates.FirstOrDefault(File.Exists);
    if (envPath is null)
        return;

    foreach (var rawLine in File.ReadAllLines(envPath, Encoding.UTF8))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            continue;

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
            continue;

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(key))
            continue;

        var existing = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(existing))
            Environment.SetEnvironmentVariable(key, value);
    }
}

static string ResolvePath(string contentRootPath, string configuredPath) =>
    Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
