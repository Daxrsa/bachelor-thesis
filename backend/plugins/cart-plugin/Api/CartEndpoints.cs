using CartPlugin.Application.Carts;
using CartPlugin.Api.Contracts;
using CartPlugin.Infrastructure.Messaging;
using CartPlugin.Infrastructure.Persistence;
using ECommerce.IntegrationContracts.V1;

namespace CartPlugin.Api;

public static class CartEndpoints
{
    private static string ResolveUserId(HttpContext context, string? routeUserId = null)
    {
        var headerUserId = context.Request.Headers["X-User-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(headerUserId))
            return headerUserId;

        if (!string.IsNullOrWhiteSpace(routeUserId) && !string.Equals(routeUserId, "me", StringComparison.OrdinalIgnoreCase))
            return routeUserId;

        throw new InvalidOperationException("Missing X-User-Id header");
    }

    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (CartsDbContext db, CancellationToken cancellationToken) =>
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? Results.Ok(new { status = "ok", plugin = "cart-plugin" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });

        app.MapGet("/carts/{userId}", async (HttpContext context, string userId, ICartService service, CancellationToken cancellationToken) =>
        {
            var resolvedUserId = ResolveUserId(context, userId);
            var cart = await service.GetByUserIdAsync(resolvedUserId, cancellationToken);
            return cart is null ? Results.NotFound(new { error = "Cart not found" }) : Results.Ok(cart.ToResponse());
        });

        app.MapGet("/carts/me", async (HttpContext context, ICartService service, CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(context);
            var cart = await service.GetByUserIdAsync(userId, cancellationToken);
            return cart is null ? Results.NotFound(new { error = "Cart not found" }) : Results.Ok(cart.ToResponse());
        });

        app.MapGet("/carts/{userId}/items/count", async (HttpContext context, string userId, ICartService service, CancellationToken cancellationToken) =>
        {
            var resolvedUserId = ResolveUserId(context, userId);
            var count = await service.CountItemsAsync(resolvedUserId, cancellationToken);
            return Results.Ok(new { count });
        });

        app.MapGet("/carts/me/items/count", async (HttpContext context, ICartService service, CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(context);
            var count = await service.CountItemsAsync(userId, cancellationToken);
            return Results.Ok(new { count });
        });

        app.MapDelete("/carts/{userId}", async (HttpContext context, string userId, ICartService service, CancellationToken cancellationToken) =>
        {
            var resolvedUserId = ResolveUserId(context, userId);
            var deleted = await service.DeleteByUserIdAsync(resolvedUserId, cancellationToken);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Cart not found" });
        });

        app.MapDelete("/carts/me", async (HttpContext context, ICartService service, CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(context);
            var deleted = await service.DeleteByUserIdAsync(userId, cancellationToken);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Cart not found" });
        });

        app.MapPost("/carts/{userId}/items", async (HttpContext context, string userId, AddProductToCartRequest request, ICartEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.ProductId))
                return Results.BadRequest(new { error = "ProductId is required" });

            if (request.Quantity <= 0)
                return Results.BadRequest(new { error = "Quantity must be greater than 0" });

            var resolvedUserId = ResolveUserId(context, userId);

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
                    UserId = resolvedUserId,
                    ProductId = request.ProductId,
                    CartId = request.CartId,
                    OrderId = null
                },
                Payload = new ProductAddedToCartEvent(
                    request.CartItemId ?? Guid.NewGuid().ToString("N"),
                    request.Quantity,
                    request.ProductId,
                    occurredAtUtc)
            };

            // Publish and return immediately; CartEventsConsumer applies the change asynchronously.
            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Accepted(value: new { eventId = envelope.EventId, status = "queued" });
        });

        app.MapPost("/carts/me/items", async (HttpContext context, AddProductToCartRequest request, ICartEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.ProductId))
                return Results.BadRequest(new { error = "ProductId is required" });

            if (request.Quantity <= 0)
                return Results.BadRequest(new { error = "Quantity must be greater than 0" });

            var userId = ResolveUserId(context);
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
                    request.ProductId,
                    occurredAtUtc)
            };

            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Accepted(value: new { eventId = envelope.EventId, status = "queued" });
        });

        app.MapDelete("/carts/{userId}/items/{productId}", async (HttpContext context, string userId, string productId, int quantity, string? cartId, string? correlationId, ICartEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(productId))
                return Results.BadRequest(new { error = "ProductId is required" });

            if (quantity <= 0)
                return Results.BadRequest(new { error = "Quantity must be greater than 0" });

            var resolvedUserId = ResolveUserId(context, userId);

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
                    UserId = resolvedUserId,
                    ProductId = productId,
                    CartId = cartId,
                    OrderId = null
                },
                Payload = new ProductRemovedFromCartEvent(
                    $"{userId}:{productId}",
                    quantity,
                    productId,
                    occurredAtUtc)
            };

            // Publish and return immediately; CartEventsConsumer applies the change asynchronously.
            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Accepted(value: new { eventId = envelope.EventId, status = "queued" });
        });

        app.MapDelete("/carts/me/items/{productId}", async (HttpContext context, string productId, int quantity, string? cartId, string? correlationId, ICartEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(productId))
                return Results.BadRequest(new { error = "ProductId is required" });

            if (quantity <= 0)
                return Results.BadRequest(new { error = "Quantity must be greater than 0" });

            var userId = ResolveUserId(context);
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
                    productId,
                    occurredAtUtc)
            };

            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Accepted(value: new { eventId = envelope.EventId, status = "queued" });
        });

        app.MapPost("/carts/me/checkout", async (HttpContext context, CheckoutCartRequest request, ICartService service, ICartEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(context);
            var cart = await service.GetByUserIdAsync(userId, cancellationToken);
            if (cart is null || cart.Items.Count == 0)
                return Results.BadRequest(new { error = "Cart is empty" });

            if (string.IsNullOrWhiteSpace(request.CurrencyCode))
                return Results.BadRequest(new { error = "CurrencyCode is required" });

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return Results.BadRequest(new { error = "PaymentMethod is required" });

            var occurredAtUtc = DateTimeOffset.UtcNow;
            var envelope = new IntegrationEventEnvelope<CartCheckoutRequestedEvent>
            {
                EventName = EventNames.CartCheckoutRequested,
                SchemaVersion = IntegrationSchemaVersions.V1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N"),
                CausationId = null,
                ResourceIds = new EventResourceIds
                {
                    UserId = userId,
                    CartId = cart.Id,
                    ProductId = null,
                    OrderId = null
                },
                Payload = new CartCheckoutRequestedEvent(
                    userId,
                    request.CurrencyCode.ToUpperInvariant(),
                    request.PaymentMethod,
                    cart.Items.Select(item => new CheckoutLineItem(item.ProductId, item.Quantity)).ToArray(),
                    occurredAtUtc)
            };

            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Accepted(value: new { eventId = envelope.EventId, status = "payment-requested" });
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
