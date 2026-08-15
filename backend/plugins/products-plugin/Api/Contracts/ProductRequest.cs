using ProductsPlugin.Domain.Entities;

namespace ProductsPlugin.Api.Contracts;

public sealed record ProductRequest(
    string? Code,
    string Name,
    string? Description,
    decimal Price,
    int Quantity,
    string InventoryStatus,
    string? Category,
    string? ImageFileName,
    string? ImageUrl,
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
        InventoryStatus = ParseInventoryStatus(InventoryStatus),
        Category = ParseCategory(Category),
        ImageFileName = ImageFileName,
        ImageUrl = ImageUrl,
        Image = ImageFileName ?? Image,
        Rating = Rating
    };

    public void ApplyTo(Product product)
    {
        product.Code = Code;
        product.Name = Name;
        product.Description = Description;
        product.Price = Price;
        product.Quantity = Quantity;
        product.InventoryStatus = ParseInventoryStatus(InventoryStatus);
        product.Category = ParseCategory(Category);
        product.ImageFileName = ImageFileName;
        product.ImageUrl = ImageUrl;
        product.Image = ImageFileName ?? Image;
        product.Rating = Rating;
    }

    private static InventoryStatus ParseInventoryStatus(string? value) =>
        Enum.TryParse<InventoryStatus>(value, ignoreCase: true, out var parsed) ? parsed : Domain.Entities.InventoryStatus.Outofstock;

    private static ProductCategory ParseCategory(string? value) =>
        Enum.TryParse<ProductCategory>(value, ignoreCase: true, out var parsed) ? parsed : ProductCategory.Electronics;
}