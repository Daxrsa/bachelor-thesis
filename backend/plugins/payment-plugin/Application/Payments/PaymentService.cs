using ECommerce.IntegrationContracts.V1;
using Microsoft.Extensions.DependencyInjection;
using PaymentPlugin.Domain.Entities;
using PaymentPlugin.Infrastructure.Messaging;

namespace PaymentPlugin.Application.Payments;

public sealed class PaymentService(
    IPaymentRepository paymentRepository,
    IProductPriceProjectionRepository productPriceRepository,
    IPaymentGateway paymentGateway,
    IPaymentEventPublisher eventPublisher) : IPaymentService
{
    public async Task HandleProductUpsertedAsync(IntegrationEventEnvelope<ProductUpsertedEvent> envelope, CancellationToken cancellationToken)
    {
        var projection = new ProductPriceProjection
        {
            ProductId = envelope.Payload.ProductId,
            Price = envelope.Payload.Price,
            CurrencyCode = envelope.Payload.CurrencyCode,
            UpdatedAtUtc = envelope.Payload.UpdatedAtUtc
        };

        await productPriceRepository.UpsertAsync(projection, cancellationToken);
    }

    public Task HandleProductDeletedAsync(IntegrationEventEnvelope<ProductDeletedEvent> envelope, CancellationToken cancellationToken) =>
        productPriceRepository.DeleteAsync(envelope.Payload.ProductId, cancellationToken);

    public async Task HandleCartCheckoutRequestedAsync(IntegrationEventEnvelope<CartCheckoutRequestedEvent> envelope, CancellationToken cancellationToken)
    {
        var existingPayment = await paymentRepository.GetByCorrelationIdAsync(envelope.CorrelationId, cancellationToken);
        if (existingPayment is not null)
            return;

        var userId = envelope.ResourceIds.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("CartCheckoutRequested requires resourceIds.userId");

        if (envelope.Payload.Items.Count == 0)
            throw new InvalidOperationException("CartCheckoutRequested requires at least one line item");

        var paidItems = new List<PaidLineItem>();
        foreach (var item in envelope.Payload.Items)
        {
            if (item.Quantity <= 0)
                throw new InvalidOperationException("CartCheckoutRequested contains an invalid line quantity");

            var product = await productPriceRepository.GetByProductIdAsync(item.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Missing projected price for product {item.ProductId}");

            if (!string.Equals(product.CurrencyCode, envelope.Payload.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Product {item.ProductId} uses currency {product.CurrencyCode}, not {envelope.Payload.CurrencyCode}");

            var lineTotal = product.Price * item.Quantity;
            paidItems.Add(new PaidLineItem(item.ProductId, item.Quantity, product.Price, lineTotal));
        }

        var amount = paidItems.Sum(item => item.LineTotal);

        var paymentId = Guid.NewGuid().ToString("N");
        var gatewayResult = await paymentGateway.ChargeAsync(
            paymentId,
            amount,
            envelope.Payload.CurrencyCode,
            envelope.Payload.PaymentMethod,
            userId,
            cancellationToken);

        var payment = new PaymentRecord
        {
            Id = paymentId,
            UserId = userId,
            CorrelationId = envelope.CorrelationId,
            Amount = amount,
            CurrencyCode = envelope.Payload.CurrencyCode,
            Status = gatewayResult.Success ? "Succeeded" : "Failed",
            PaymentMethod = envelope.Payload.PaymentMethod,
            ProviderReference = gatewayResult.ProviderReference,
            FailureReason = gatewayResult.FailureReason,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await paymentRepository.AddAsync(payment, cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        if (gatewayResult.Success)
        {
            var succeeded = new IntegrationEventEnvelope<PaymentSucceededEvent>
            {
                EventName = EventNames.PaymentSucceeded,
                SchemaVersion = IntegrationSchemaVersions.V1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = DateTimeOffset.UtcNow,
                CorrelationId = envelope.CorrelationId,
                CausationId = envelope.EventId.ToString(),
                ResourceIds = new EventResourceIds
                {
                    UserId = userId,
                    ProductId = null,
                    CartId = envelope.ResourceIds.CartId,
                    PaymentId = payment.Id,
                    OrderId = envelope.ResourceIds.OrderId
                },
                Payload = new PaymentSucceededEvent(
                    payment.Id,
                    gatewayResult.ProviderReference,
                    payment.Amount,
                    payment.CurrencyCode,
                    payment.PaymentMethod,
                    paidItems,
                    DateTimeOffset.UtcNow)
            };

            await eventPublisher.PublishAsync(succeeded, cancellationToken);
            return;
        }

        var failed = new IntegrationEventEnvelope<PaymentFailedEvent>
        {
            EventName = EventNames.PaymentFailed,
            SchemaVersion = IntegrationSchemaVersions.V1,
            EventId = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.EventId.ToString(),
            ResourceIds = new EventResourceIds
            {
                UserId = userId,
                ProductId = null,
                CartId = envelope.ResourceIds.CartId,
                PaymentId = payment.Id,
                OrderId = envelope.ResourceIds.OrderId
            },
            Payload = new PaymentFailedEvent(
                payment.Id,
                gatewayResult.FailureReason ?? "Payment failed",
                DateTimeOffset.UtcNow)
        };

        await eventPublisher.PublishAsync(failed, cancellationToken);
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
