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
