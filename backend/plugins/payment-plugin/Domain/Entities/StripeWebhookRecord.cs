namespace PaymentPlugin.Domain.Entities;

public sealed class StripeWebhookRecord
{
    public required string EventId { get; set; }
    public required string EventType { get; set; }
    public string? ProviderReference { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset StripeCreatedAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}