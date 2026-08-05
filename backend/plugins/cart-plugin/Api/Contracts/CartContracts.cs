namespace CartPlugin.Api.Contracts;

public sealed record AddProductToCartRequest(
    string ProductId,
    int Quantity,
    string? CartId,
    string? CartItemId,
    string? CorrelationId);

public sealed record CheckoutCartRequest(
    string CurrencyCode,
    string PaymentMethod,
    string? CorrelationId);

public sealed record CartResponse(
    string Id,
    string UserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<CartItemResponse> Items);

public sealed record CartItemResponse(
    string Id,
    string ProductId,
    int Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
