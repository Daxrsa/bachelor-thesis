using ProductsPlugin.Domain.Entities;

namespace ProductsPlugin.Application.Contracts;

public sealed record ProductRequest(
    string? Code,
    string Name,
    string? Description,
    decimal Price,
    int Quantity,
    string InventoryStatus,
    string? Category,
    string? Image,
    int Rating)
{
    public Product ToProduct(string id) => new()
    {
        Id = id,
        Code = Code,
        Name = Name,
        Description = Description,
        Price = Price,
        Quantity = Quantity,
        InventoryStatus = InventoryStatus,
        Category = Category,
        Image = Image,
        Rating = Rating
    };

    public void ApplyTo(Product product)
    {
        product.Code = Code;
        product.Name = Name;
        product.Description = Description;
        product.Price = Price;
        product.Quantity = Quantity;
        product.InventoryStatus = InventoryStatus;
        product.Category = Category;
        product.Image = Image;
        product.Rating = Rating;
    }
}