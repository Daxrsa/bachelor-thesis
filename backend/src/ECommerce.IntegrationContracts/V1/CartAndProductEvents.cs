namespace ECommerce.IntegrationContracts.V1;

public sealed record ProductAddedToCartEvent(
    string CartItemId,
    int Quantity,
    string ProductId,
    DateTimeOffset AddedAtUtc);

public sealed record ProductRemovedFromCartEvent(
    string CartItemId,
    int Quantity,
    string ProductId,
    DateTimeOffset RemovedAtUtc);

public sealed record CheckoutLineItem(
    string ProductId,
    int Quantity);

public sealed record CartCheckoutRequestedEvent(
    string UserId,
    string CurrencyCode,
    string PaymentMethod,
    IReadOnlyList<CheckoutLineItem> Items,
    DateTimeOffset RequestedAtUtc);

public sealed record ProductUpsertedEvent(
    string ProductId,
    decimal Price,
    string CurrencyCode,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductDeletedEvent(
    string ProductId,
    DateTimeOffset DeletedAtUtc);
