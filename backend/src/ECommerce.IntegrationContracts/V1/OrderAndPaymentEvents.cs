namespace ECommerce.IntegrationContracts.V1;

public sealed record OrderPaymentRequestedEvent(
    string UserId,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethod,
    DateTimeOffset RequestedAtUtc);

public sealed record PaymentSucceededEvent(
    string PaymentId,
    string ProviderReference,
    decimal Amount,
    string CurrencyCode,
    DateTimeOffset PaidAtUtc);

public sealed record PaymentFailedEvent(
    string PaymentId,
    string Reason,
    DateTimeOffset FailedAtUtc);
