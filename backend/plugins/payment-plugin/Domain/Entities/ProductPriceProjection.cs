namespace PaymentPlugin.Domain.Entities;

public sealed class ProductPriceProjection
{
    public required string ProductId { get; set; }
    public decimal Price { get; set; }
    public required string CurrencyCode { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
