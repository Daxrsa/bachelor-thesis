using CartPlugin.Application.Carts;
using CartPlugin.Api.Contracts;
using CartPlugin.Infrastructure.Messaging;
using CartPlugin.Infrastructure.Persistence;
using ECommerce.IntegrationContracts.V1;

namespace CartPlugin.Api;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (CartsDbContext db, CancellationToken cancellationToken) =>
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? Results.Ok(new { status = "ok", plugin = "cart-plugin" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });

        app.MapGet("/carts/{userId}", async (string userId, ICartService service, CancellationToken cancellationToken) =>
        {
            var cart = await service.GetByUserIdAsync(userId, cancellationToken);
            return cart is null ? Results.NotFound(new { error = "Cart not found" }) : Results.Ok(cart.ToResponse());
        });

        app.MapPost("/carts/{userId}/items", async (string userId, AddProductToCartRequest request, ICartEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (request.Quantity <= 0)
                return Results.BadRequest(new { error = "Quantity must be greater than 0" });

            var occurredAtUtc = DateTimeOffset.UtcNow;
            var envelope = new IntegrationEventEnvelope<ProductAddedToCartEvent>
            {
                EventName = EventNames.ProductAddedToCart,
                SchemaVersion = IntegrationSchemaVersions.V1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N"),
                CausationId = null,
                ResourceIds = new EventResourceIds
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    CartId = request.CartId,
                    OrderId = null
                },
                Payload = new ProductAddedToCartEvent(
                    request.CartItemId ?? Guid.NewGuid().ToString("N"),
                    request.Quantity,
                    occurredAtUtc)
            };

            // Publish and return immediately; CartEventsConsumer applies the change asynchronously.
            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Accepted(value: new { eventId = envelope.EventId, status = "queued" });
        });

        app.MapDelete("/carts/{userId}/items/{productId}", async (string userId, string productId, int quantity, string? cartId, string? correlationId, ICartEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (quantity <= 0)
                return Results.BadRequest(new { error = "Quantity must be greater than 0" });

            var occurredAtUtc = DateTimeOffset.UtcNow;
            var envelope = new IntegrationEventEnvelope<ProductRemovedFromCartEvent>
            {
                EventName = EventNames.ProductRemovedFromCart,
                SchemaVersion = IntegrationSchemaVersions.V1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
                CausationId = null,
                ResourceIds = new EventResourceIds
                {
                    UserId = userId,
                    ProductId = productId,
                    CartId = cartId,
                    OrderId = null
                },
                Payload = new ProductRemovedFromCartEvent(
                    $"{userId}:{productId}",
                    quantity,
                    occurredAtUtc)
            };

            // Publish and return immediately; CartEventsConsumer applies the change asynchronously.
            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Accepted(value: new { eventId = envelope.EventId, status = "queued" });
        });

        app.MapGet("/cart/greeting", (HttpContext ctx) =>
        {
            var email = ctx.Request.Headers["X-User-Email"].ToString();
            return Results.Ok(new
            {
                message = string.IsNullOrEmpty(email) ? "Hello, guest!" : $"Hello, {email}!",
                from = "cart-plugin"
            });
        });

        return app;
    }
}
