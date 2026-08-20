using OrderPlugin.Application.Orders;
using OrderPlugin.Domain.Entities;
using OrderPlugin.Infrastructure.Persistence;

namespace OrderPlugin.Api;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (OrdersDbContext db, CancellationToken cancellationToken) =>
            await db.Database.CanConnectAsync(cancellationToken)
                ? Results.Ok(new { status = "ok", plugin = "order-plugin" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapGet("/orders/greeting", (HttpContext context) =>
        {
            var email = context.Request.Headers["X-User-Email"].ToString();
            return Results.Ok(new
            {
                message = string.IsNullOrWhiteSpace(email) ? "Hello, guest!" : $"Hello, {email}!",
                from = "order-plugin"
            });
        });

        app.MapGet("/orders", async (IOrderService service, CancellationToken cancellationToken) =>
        {
            var orders = await service.ListAllAsync(cancellationToken);
            return Results.Ok(orders.Select(ToResponse));
        });

        app.MapGet("/orders/me", async (HttpContext context, IOrderService service, CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(context);
            var orders = await service.ListByUserIdAsync(userId, cancellationToken);
            return Results.Ok(orders.Select(ToResponse));
        });

        app.MapGet("/orders/me/{orderId}", async (HttpContext context, string orderId, IOrderService service, CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(context);
            var order = await service.GetByIdForUserAsync(orderId, userId, cancellationToken);
            return order is null ? Results.NotFound(new { error = "Order not found" }) : Results.Ok(ToResponse(order));
        });

        app.MapPatch("/orders/{orderId}/status", async (string orderId, UpdateOrderStatusRequest request, IOrderService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var order = await service.UpdateStatusAsync(orderId, request.Status, cancellationToken);
                return order is null
                    ? Results.NotFound(new { error = "Order not found" })
                    : Results.Ok(ToResponse(order));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }

    private static string ResolveUserId(HttpContext context)
    {
        var userId = context.Request.Headers["X-User-Id"].ToString();
        return string.IsNullOrWhiteSpace(userId)
            ? throw new InvalidOperationException("Missing X-User-Id header")
            : userId;
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.UserId,
        order.CartId,
        order.PaymentId,
        order.OrderStatus.ToString(),
        order.TotalAmount,
        order.CurrencyCode,
        order.PaymentMethod,
        order.ProviderReference,
        order.PaidAtUtc,
        order.CreatedAtUtc,
        order.Items.Select(item => new OrderItemResponse(item.ProductId, item.Quantity, item.UnitPrice, item.LineTotal)).ToArray());
}

public sealed record OrderResponse(
    string Id,
    string UserId,
    string CartId,
    string PaymentId,
    string Status,
    decimal TotalAmount,
    string CurrencyCode,
    string PaymentMethod,
    string ProviderReference,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(string ProductId, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record UpdateOrderStatusRequest(string Status);
