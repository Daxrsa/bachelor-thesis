namespace ECommerce.IntegrationContracts.V1;

public sealed record ProductAddedToCartEvent(
    string CartItemId,
    int Quantity,
    DateTimeOffset AddedAtUtc);

public sealed record ProductRemovedFromCartEvent(
    string CartItemId,
    int Quantity,
    DateTimeOffset RemovedAtUtc);
