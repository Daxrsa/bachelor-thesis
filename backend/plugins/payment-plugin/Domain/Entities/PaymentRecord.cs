namespace PaymentPlugin.Domain.Entities;

public sealed class PaymentRecord
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string CorrelationId { get; set; }
    public required decimal Amount { get; set; }
    public required string CurrencyCode { get; set; }
    public required string Status { get; set; }
    public required string PaymentMethod { get; set; }
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
