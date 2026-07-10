var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Every plugin must expose /health so the core can gate rollout on readiness.
app.MapGet("/health", () => Results.Ok(new { status = "ok", plugin = "hello-plugin" }));

// Business endpoint — invoked via the core at /api/p/hello-plugin/greeting
app.MapGet("/greeting", (HttpContext ctx) =>
{
    var email = ctx.Request.Headers["X-User-Email"].ToString();
    return Results.Ok(new
    {
        message = string.IsNullOrEmpty(email) ? "Hello, guest!" : $"Hello, {email}!",
        from = "hello-plugin"
    });
});

app.Run("http://0.0.0.0:8080");
