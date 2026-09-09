var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Every plugin must expose /health so the core can gate rollout on readiness.
app.MapGet("/health", () => Results.Ok(new { status = "ok", plugin = "banner-plugin" }));

// Business endpoint — invoked via the core at /api/p/banner-plugin/banner
app.MapGet("/banner", (HttpContext ctx) =>
{
    var email = ctx.Request.Headers["X-User-Email"].ToString();
    return Results.Ok(new
    {
        title = "Store banner",
        message = "Free shipping this week.",
        greeting = string.IsNullOrEmpty(email) ? "Hello, guest!" : $"Hello, {email}!",
        from = "banner-plugin"
    });
});

app.Run("http://0.0.0.0:8080");
