namespace OrderPlugin.Domain.Entities;

public sealed class Order
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string CartId { get; set; }
    public required string PaymentId { get; set; }
    public required string CorrelationId { get; set; }
    public required OrderStatus OrderStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public required string CurrencyCode { get; set; }
    public required string PaymentMethod { get; set; }
    public required string ProviderReference { get; set; }
    public DateTimeOffset PaidAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public required string Id { get; set; }
    public required string OrderId { get; set; }
    public required string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public enum OrderStatus
{
    Processed,
    Accepted,
    Dispatched, 
    Completed
}