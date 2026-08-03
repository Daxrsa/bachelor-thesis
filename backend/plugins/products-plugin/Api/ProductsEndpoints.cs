using ProductsPlugin.Api.Contracts;
using ProductsPlugin.Application.Products;
using ProductsPlugin.Infrastructure.Persistence;

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

        app.MapPost("/products", async (ProductRequest request, IProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/products/{product.Id}", product);
        });

        app.MapPut("/products/{id}", async (string id, ProductRequest request, IProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.UpdateAsync(id, request, cancellationToken);
            return product is null ? Results.NotFound(new { error = "Product not found" }) : Results.Ok(product);
        });

        app.MapDelete("/products/{id}", async (string id, IProductService service, CancellationToken cancellationToken) =>
        {
            var wasDeleted = await service.DeleteAsync(id, cancellationToken);
            return wasDeleted ? Results.NoContent() : Results.NotFound(new { error = "Product not found" });
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