using ProductsPlugin.Api.Contracts;
using ProductsPlugin.Application.Products;
using ProductsPlugin.Infrastructure.Messaging;
using ProductsPlugin.Infrastructure.Persistence;
using ECommerce.IntegrationContracts.V1;

namespace ProductsPlugin.Api;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (ProductsDbContext db, CancellationToken cancellationToken) =>
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? Results.Ok(new { status = "ok", plugin = "products-plugin" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });

        app.MapGet("/products", (IProductService service, decimal? minPrice, decimal? maxPrice, string[]? availability, CancellationToken cancellationToken) =>
            service.ListAsync(new ProductFilter(minPrice, maxPrice, availability), cancellationToken));

        app.MapGet("/products/{id}", async (string id, IProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.GetByIdAsync(id, cancellationToken);
            return product is null ? Results.NotFound(new { error = "Product not found" }) : Results.Ok(product);
        });

        app.MapPost("/products", async (HttpContext context, ProductRequest request, IProductService service, IProductEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            var product = await service.CreateAsync(request, cancellationToken);
            var occurredAtUtc = DateTimeOffset.UtcNow;
            var userId = context.Request.Headers["X-User-Id"].ToString();

            var envelope = new IntegrationEventEnvelope<ProductUpsertedEvent>
            {
                EventName = EventNames.ProductUpserted,
                SchemaVersion = IntegrationSchemaVersions.V1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                CorrelationId = Guid.NewGuid().ToString("N"),
                CausationId = null,
                ResourceIds = new EventResourceIds
                {
                    ProductId = product.Id,
                    UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                    CartId = null,
                    OrderId = null
                },
                Payload = new ProductUpsertedEvent(product.Id, product.Price, "USD", occurredAtUtc)
            };

            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Created($"/products/{product.Id}", product);
        });

        app.MapPut("/products/{id}", async (HttpContext context, string id, ProductRequest request, IProductService service, IProductEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            var product = await service.UpdateAsync(id, request, cancellationToken);
            if (product is null)
                return Results.NotFound(new { error = "Product not found" });

            var occurredAtUtc = DateTimeOffset.UtcNow;
            var userId = context.Request.Headers["X-User-Id"].ToString();
            var envelope = new IntegrationEventEnvelope<ProductUpsertedEvent>
            {
                EventName = EventNames.ProductUpserted,
                SchemaVersion = IntegrationSchemaVersions.V1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                CorrelationId = Guid.NewGuid().ToString("N"),
                CausationId = null,
                ResourceIds = new EventResourceIds
                {
                    ProductId = product.Id,
                    UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                    CartId = null,
                    OrderId = null
                },
                Payload = new ProductUpsertedEvent(product.Id, product.Price, "USD", occurredAtUtc)
            };

            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Ok(product);
        });

        app.MapDelete("/products/{id}", async (HttpContext context, string id, IProductService service, IProductEventPublisher publisher, CancellationToken cancellationToken) =>
        {
            var wasDeleted = await service.DeleteAsync(id, cancellationToken);
            if (!wasDeleted)
                return Results.NotFound(new { error = "Product not found" });

            var occurredAtUtc = DateTimeOffset.UtcNow;
            var userId = context.Request.Headers["X-User-Id"].ToString();
            var envelope = new IntegrationEventEnvelope<ProductDeletedEvent>
            {
                EventName = EventNames.ProductDeleted,
                SchemaVersion = IntegrationSchemaVersions.V1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                CorrelationId = Guid.NewGuid().ToString("N"),
                CausationId = null,
                ResourceIds = new EventResourceIds
                {
                    ProductId = id,
                    UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                    CartId = null,
                    OrderId = null
                },
                Payload = new ProductDeletedEvent(id, occurredAtUtc)
            };

            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.NoContent();
        });

        app.MapGet("/products/greeting", (HttpContext ctx) =>
        {
            var email = ctx.Request.Headers["X-User-Email"].ToString();
            return Results.Ok(new
            {
                message = string.IsNullOrEmpty(email) ? "Hello, guest!" : $"Hello, {email}!",
                from = "products-plugin"
            });
        });

        return app;
    }
}