namespace PaymentPlugin.Application.Payments;

public sealed record PaymentResult(
    bool Success,
    string ProviderReference,
    string? FailureReason);
