using ECommerce.IntegrationContracts.V1;
using Microsoft.Extensions.DependencyInjection;
using OrderPlugin.Domain.Entities;
using OrderPlugin.Infrastructure.Messaging;

namespace OrderPlugin.Application.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken);
    Task<Order?> GetByIdForUserAsync(string orderId, string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IOrderService
{
    Task<Order> HandlePaymentSucceededAsync(IntegrationEventEnvelope<PaymentSucceededEvent> envelope, CancellationToken cancellationToken);
    Task<Order?> GetByIdForUserAsync(string orderId, string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListByUserIdAsync(string userId, CancellationToken cancellationToken);
}

public sealed class OrderService(IOrderRepository repository, IOrderEventPublisher eventPublisher) : IOrderService
{
    public async Task<Order> HandlePaymentSucceededAsync(IntegrationEventEnvelope<PaymentSucceededEvent> envelope, CancellationToken cancellationToken)
    {
        var userId = Require(envelope.ResourceIds.UserId, "PaymentSucceeded requires resourceIds.userId");
        var cartId = Require(envelope.ResourceIds.CartId, "PaymentSucceeded requires resourceIds.cartId");
        var paymentId = Require(envelope.ResourceIds.PaymentId, "PaymentSucceeded requires resourceIds.paymentId");

        if (!string.Equals(paymentId, envelope.Payload.PaymentId, StringComparison.Ordinal))
            throw new InvalidOperationException("PaymentSucceeded payment ID does not match resourceIds.paymentId");
        if (envelope.Payload.Items.Count == 0)
            throw new InvalidOperationException("PaymentSucceeded requires at least one paid line item");
        if (envelope.Payload.Items.Any(item => item.Quantity <= 0 || item.UnitPrice < 0 || item.LineTotal != item.UnitPrice * item.Quantity))
            throw new InvalidOperationException("PaymentSucceeded contains an invalid paid line item");

        var calculatedTotal = envelope.Payload.Items.Sum(item => item.LineTotal);
        if (calculatedTotal != envelope.Payload.Amount)
            throw new InvalidOperationException("PaymentSucceeded amount does not match paid line total");

        var existing = await repository.GetByPaymentIdAsync(paymentId, cancellationToken);
        if (existing is not null)
            return existing;

        var order = new Order
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            CartId = cartId,
            PaymentId = paymentId,
            CorrelationId = envelope.CorrelationId,
            Status = "Paid",
            TotalAmount = envelope.Payload.Amount,
            CurrencyCode = envelope.Payload.CurrencyCode,
            PaymentMethod = envelope.Payload.PaymentMethod,
            ProviderReference = envelope.Payload.ProviderReference,
            PaidAtUtc = envelope.Payload.PaidAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Items = envelope.Payload.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid().ToString("N"),
                OrderId = string.Empty,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList()
        };
        foreach (var item in order.Items)
            item.OrderId = order.Id;

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(new IntegrationEventEnvelope<OrderCreatedEvent>
        {
            EventName = EventNames.OrderCreated,
            SchemaVersion = IntegrationSchemaVersions.V1,
            EventId = Guid.NewGuid(),
            OccurredAtUtc = order.CreatedAtUtc,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.EventId.ToString(),
            ResourceIds = new EventResourceIds
            {
                UserId = order.UserId,
                CartId = order.CartId,
                PaymentId = order.PaymentId,
                OrderId = order.Id
            },
            Payload = new OrderCreatedEvent(
                order.Id,
                order.UserId,
                order.CartId,
                order.PaymentId,
                order.TotalAmount,
                order.CurrencyCode,
                order.CreatedAtUtc)
        }, cancellationToken);
        return order;
    }

    public Task<Order?> GetByIdForUserAsync(string orderId, string userId, CancellationToken cancellationToken) =>
        repository.GetByIdForUserAsync(orderId, userId, cancellationToken);

    public Task<IReadOnlyList<Order>> ListByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        repository.ListByUserIdAsync(userId, cancellationToken);

    private static string Require(string? value, string error) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(error) : value;
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderApplication(this IServiceCollection services) =>
        services.AddScoped<IOrderService, OrderService>();
}
