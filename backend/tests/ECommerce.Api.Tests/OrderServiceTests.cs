using ECommerce.IntegrationContracts.V1;
using OrderPlugin.Application.Orders;
using OrderPlugin.Domain.Entities;
using OrderPlugin.Infrastructure.Messaging;
using Xunit;

namespace ECommerce.Api.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task HandlePaymentSucceededAsync_CreatesCompleteOrderOnce()
    {
        var repository = new FakeOrderRepository();
        var publisher = new FakeOrderEventPublisher();
        var service = new OrderService(repository, publisher);
        var payment = CreatePaymentSucceeded("payment-1", "checkout-1");

        var first = await service.HandlePaymentSucceededAsync(payment, CancellationToken.None);
        var duplicate = await service.HandlePaymentSucceededAsync(payment, CancellationToken.None);

        Assert.Equal(first.Id, duplicate.Id);
        Assert.Single(repository.Orders);
        Assert.Equal("user-1", first.UserId);
        Assert.Equal("cart-1", first.CartId);
        Assert.Equal("payment-1", first.PaymentId);
        Assert.Equal(25m, first.TotalAmount);
        Assert.Equal(2, first.Items.Count);
        Assert.Single(publisher.Events);
        Assert.Equal(first.Id, publisher.Events[0].Payload.OrderId);
        Assert.Equal("cart-1", publisher.Events[0].ResourceIds.CartId);
    }

    [Fact]
    public async Task HandlePaymentSucceededAsync_RejectsMismatchedTotal()
    {
        var service = new OrderService(new FakeOrderRepository(), new FakeOrderEventPublisher());
        var payment = CreatePaymentSucceeded("payment-1", "checkout-1") with
        {
            Payload = new PaymentSucceededEvent(
                "payment-1", "provider-1", 99m, "USD", "card",
                [new PaidLineItem("product-1", 1, 25m, 25m)], DateTimeOffset.UtcNow)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.HandlePaymentSucceededAsync(payment, CancellationToken.None));
    }

    [Fact]
    public async Task HandlePaymentSucceededAsync_UsesPayloadPaymentIdWhenResourceIdIsMissing()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository, new FakeOrderEventPublisher());
        var payment = CreatePaymentSucceeded("payment-1", "checkout-1") with
        {
            ResourceIds = new EventResourceIds { UserId = "user-1", CartId = "cart-1" }
        };

        var order = await service.HandlePaymentSucceededAsync(payment, CancellationToken.None);

        Assert.Equal("payment-1", order.PaymentId);
        Assert.Single(repository.Orders);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesExistingOrder()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository, new FakeOrderEventPublisher());
        var created = await service.HandlePaymentSucceededAsync(CreatePaymentSucceeded("payment-1", "checkout-1"), CancellationToken.None);

        var updated = await service.UpdateStatusAsync(created.Id, "Dispatched", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(OrderStatus.Dispatched, updated!.OrderStatus);
        Assert.Equal(OrderStatus.Dispatched, repository.Orders[0].OrderStatus);
    }

    private static IntegrationEventEnvelope<PaymentSucceededEvent> CreatePaymentSucceeded(string paymentId, string correlationId) => new()
    {
        EventName = EventNames.PaymentSucceeded,
        SchemaVersion = IntegrationSchemaVersions.V1,
        EventId = Guid.NewGuid(),
        OccurredAtUtc = DateTimeOffset.UtcNow,
        CorrelationId = correlationId,
        CausationId = null,
        ResourceIds = new EventResourceIds { UserId = "user-1", CartId = "cart-1", PaymentId = paymentId },
        Payload = new PaymentSucceededEvent(
            paymentId,
            "provider-1",
            25m,
            "USD",
            "card",
            [new PaidLineItem("product-1", 1, 20m, 20m), new PaidLineItem("product-2", 1, 5m, 5m)],
            DateTimeOffset.UtcNow)
    };

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public List<Order> Orders { get; } = [];

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken) =>
            Task.FromResult<Order?>(Orders.FirstOrDefault(order => order.PaymentId == paymentId));

        public Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken) =>
            Task.FromResult<Order?>(Orders.FirstOrDefault(order => order.Id == orderId));

        public Task<Order?> GetByIdForUserAsync(string orderId, string userId, CancellationToken cancellationToken) =>
            Task.FromResult<Order?>(Orders.FirstOrDefault(order => order.Id == orderId && order.UserId == userId));

        public Task<IReadOnlyList<Order>> ListByUserIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(Orders.Where(order => order.UserId == userId).ToList());

        public Task<IReadOnlyList<Order>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(Orders.ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeOrderEventPublisher : IOrderEventPublisher
    {
        public List<IntegrationEventEnvelope<OrderCreatedEvent>> Events { get; } = [];

        public Task PublishAsync(IntegrationEventEnvelope<OrderCreatedEvent> envelope, CancellationToken cancellationToken)
        {
            Events.Add(envelope);
            return Task.CompletedTask;
        }
    }
}
