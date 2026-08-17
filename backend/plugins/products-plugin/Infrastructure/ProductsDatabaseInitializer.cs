using Microsoft.EntityFrameworkCore;
using ProductsPlugin.Domain.Entities;
using ProductsPlugin.Infrastructure.Persistence;

namespace ProductsPlugin.Infrastructure;

public sealed class ProductsDatabaseInitializer(ProductsDbContext db)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                if (!await db.Products.AnyAsync(cancellationToken))
                {
                    db.Products.AddRange(ProductSeed.All);
                    await db.SaveChangesAsync(cancellationToken);
                }

                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new InvalidOperationException("Products database did not become ready within 30 seconds", lastError);
    }
}

internal static class ProductSeed
{
    public static readonly Product[] All =
    {
        new() { Id = "1000", Code = "f230fh0g3", Name = "Smartwatch Watch", Description = "Product Description", Image = "", Price = 65, Category = ProductCategory.Electronics, Quantity = 24, InventoryStatus = InventoryStatus.Lowstock, Rating = 5 },
        new() { Id = "1001", Code = "nvklal433", Name = "Fitbit", Description = "Product Description", Image = "", Price = 72, Category = ProductCategory.Accessories, Quantity = 61, InventoryStatus = InventoryStatus.Instock, Rating = 4 },
        new() { Id = "1002", Code = "zz21cz3c1", Name = "MacBook Pro", Description = "Product Description", Image = "", Price = 79, Category = ProductCategory.Electronics, Quantity = 2, InventoryStatus = InventoryStatus.Lowstock, Rating = 3 },
        new() { Id = "1003", Code = "244wgerg2", Name = "Samsung OLED", Description = "Product Description", Image = "", Price = 29, Category = ProductCategory.Electronics, Quantity = 25, InventoryStatus = InventoryStatus.Instock, Rating = 5 },
        new() { Id = "1004", Code = "h456wer53", Name = "JBL Buds 4 Pro", Description = "Product Description", Image = "", Price = 15, Category = ProductCategory.Electronics, Quantity = 73, InventoryStatus = InventoryStatus.Instock, Rating = 4 },
        new() { Id = "1005", Code = "av2231fwg", Name = "Sony Headphones Premium", Description = "Product Description", Image = "", Price = 120, Category = ProductCategory.Electronics, Quantity = 0, InventoryStatus = InventoryStatus.Outofstock, Rating = 4 },
        new() { Id = "1006", Code = "bib36pfvm", Name = "Docking Station Satechi Hub", Description = "Product Description", Image = "", Price = 32, Category = ProductCategory.Accessories, Quantity = 5, InventoryStatus = InventoryStatus.Lowstock, Rating = 3 },
        new() { Id = "1007", Code = "mbvjkgip5", Name = "Magic Keyboard", Description = "Product Description", Image = "", Price = 34, Category = ProductCategory.Electronics, Quantity = 23, InventoryStatus = InventoryStatus.Instock, Rating = 5 },
        new() { Id = "1008", Code = "vbb124btr", Name = "Logitech Mouse", Description = "Product Description", Image = "", Price = 99, Category = ProductCategory.Accessories, Quantity = 2, InventoryStatus = InventoryStatus.Instock, Rating = 4 },
        new() { Id = "1009", Code = "cm230f032", Name = "Intel i7 Processor", Description = "Product Description", Image = "", Price = 299, Category = ProductCategory.Electronics, Quantity = 63, InventoryStatus = InventoryStatus.Lowstock, Rating = 3 }
    };
}