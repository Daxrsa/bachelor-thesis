using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Domain.Entities;
using PaymentPlugin.Infrastructure.Persistence;
using Stripe;
using System.Text.Json;

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

        app.MapGet("/webhooks/stripe/events", async (PaymentsDbContext db, CancellationToken cancellationToken) =>
        {
            var events = await db.StripeWebhookEvents
                .AsNoTracking()
                .OrderByDescending(webhook => webhook.ReceivedAtUtc)
                .Take(100)
                .ToListAsync(cancellationToken);

            return Results.Ok(events);
        });

        app.MapPost("/webhooks/stripe", async (HttpContext context, IConfiguration configuration, PaymentsDbContext db) =>
        {
            var webhookSecret = configuration["STRIPE_WEBHOOK_SECRET"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
                return Results.Problem(
                    detail: "Stripe webhook secret is not configured",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var signature = context.Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
                return Results.BadRequest(new { error = "Missing Stripe-Signature header" });

            using var reader = new StreamReader(context.Request.Body);
            var payload = await reader.ReadToEndAsync(context.RequestAborted);

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);

                if (!await db.StripeWebhookEvents.AnyAsync(webhook => webhook.EventId == stripeEvent.Id, context.RequestAborted))
                {
                    using var document = JsonDocument.Parse(payload);
                    var root = document.RootElement;
                    var stripeObject = root.GetProperty("data").GetProperty("object");
                    var amountInMinorUnits = GetInt64(stripeObject, "amount_received") ?? GetInt64(stripeObject, "amount");
                    var createdUnixSeconds = GetInt64(root, "created");

                    db.StripeWebhookEvents.Add(new StripeWebhookRecord
                    {
                        EventId = stripeEvent.Id,
                        EventType = stripeEvent.Type,
                        ProviderReference = GetString(stripeObject, "id"),
                        CustomerId = GetString(stripeObject, "customer"),
                        CustomerEmail = GetString(stripeObject, "receipt_email"),
                        Amount = amountInMinorUnits.HasValue ? amountInMinorUnits.Value / 100m : null,
                        CurrencyCode = GetString(stripeObject, "currency")?.ToUpperInvariant(),
                        Status = GetString(stripeObject, "status"),
                        StripeCreatedAtUtc = createdUnixSeconds.HasValue
                            ? DateTimeOffset.FromUnixTimeSeconds(createdUnixSeconds.Value)
                            : DateTimeOffset.UtcNow,
                        ReceivedAtUtc = DateTimeOffset.UtcNow
                    });
                    await db.SaveChangesAsync(context.RequestAborted);
                }

                return Results.Ok(new { received = true, eventId = stripeEvent.Id, eventType = stripeEvent.Type });
            }
            catch (Exception exception) when (exception is StripeException or JsonException)
            {
                return Results.BadRequest(new { error = "Invalid Stripe webhook signature" });
            }
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

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static long? GetInt64(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var result)
            ? result
            : null;
    }
}
