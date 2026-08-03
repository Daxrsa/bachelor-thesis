namespace CartPlugin.Domain.Entities;

public sealed class CartItem
{
    public required string Id { get; set; }
    public required string CartId { get; set; }
    public required string ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
