namespace CartPlugin.Domain.Entities;

public sealed class Cart
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<CartItem> Items { get; set; } = new();
}
