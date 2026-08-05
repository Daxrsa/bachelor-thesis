using ECommerce.IntegrationContracts.V1;

namespace PaymentPlugin.Application.Payments;

public interface IPaymentService
{
    Task HandleProductUpsertedAsync(IntegrationEventEnvelope<ProductUpsertedEvent> envelope, CancellationToken cancellationToken);
    Task HandleProductDeletedAsync(IntegrationEventEnvelope<ProductDeletedEvent> envelope, CancellationToken cancellationToken);
    Task HandleCartCheckoutRequestedAsync(IntegrationEventEnvelope<CartCheckoutRequestedEvent> envelope, CancellationToken cancellationToken);
}
