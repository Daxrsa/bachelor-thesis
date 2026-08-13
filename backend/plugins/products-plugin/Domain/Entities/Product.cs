namespace ProductsPlugin.Domain.Entities;

public sealed class Product
{
    public required string Id { get; set; }
    public string? Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public required string InventoryStatus { get; set; }
    public string? Category { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageUrl { get; set; }
    public string? Image { get; set; }
    public int Rating { get; set; }
}