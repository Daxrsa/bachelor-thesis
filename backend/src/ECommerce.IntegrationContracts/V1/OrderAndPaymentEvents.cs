namespace ECommerce.IntegrationContracts.V1;

public sealed record OrderPaymentRequestedEvent(
    string UserId,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethod,
    DateTimeOffset RequestedAtUtc);

public sealed record PaidLineItem(
    string ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PaymentSucceededEvent(
    string PaymentId,
    string ProviderReference,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethod,
    IReadOnlyList<PaidLineItem> Items,
    DateTimeOffset PaidAtUtc);

public sealed record PaymentFailedEvent(
    string PaymentId,
    string Reason,
    DateTimeOffset FailedAtUtc);

public sealed record OrderCreatedEvent(
    string OrderId,
    string UserId,
    string CartId,
    string PaymentId,
    decimal TotalAmount,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc);
