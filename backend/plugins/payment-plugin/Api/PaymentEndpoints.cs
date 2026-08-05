using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Infrastructure.Persistence;

namespace PaymentPlugin.Api;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (PaymentsDbContext db, CancellationToken cancellationToken) =>
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? Results.Ok(new { status = "ok", plugin = "payment-plugin" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });

        app.MapGet("/payments/me", async (HttpContext context, PaymentsDbContext db, CancellationToken cancellationToken) =>
        {
            var userId = context.Request.Headers["X-User-Id"].ToString();
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest(new { error = "Missing X-User-Id header" });

            var payments = await db.Payments
                .Where(payment => payment.UserId == userId)
                .OrderByDescending(payment => payment.CreatedAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

            return Results.Ok(payments);
        });

        app.MapGet("/payment/greeting", (HttpContext context) =>
        {
            var email = context.Request.Headers["X-User-Email"].ToString();
            return Results.Ok(new
            {
                message = string.IsNullOrWhiteSpace(email) ? "Hello, guest!" : $"Hello, {email}!",
                from = "payment-plugin"
            });
        });

        return app;
    }
}
