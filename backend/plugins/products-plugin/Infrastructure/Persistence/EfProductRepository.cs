using Microsoft.EntityFrameworkCore;
using ProductsPlugin.Application.Products;
using ProductsPlugin.Domain.Entities;

namespace ProductsPlugin.Infrastructure.Persistence;

public sealed class EfProductRepository(ProductsDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();

        if (filter.MinPrice is not null)
            query = query.Where(product => product.Price >= filter.MinPrice);

        if (filter.MaxPrice is not null)
            query = query.Where(product => product.Price <= filter.MaxPrice);

        if (filter.Availability is { Length: > 0 })
            query = query.Where(product => filter.Availability.Contains(product.InventoryStatus));

        return await query.OrderBy(product => product.Name).ToListAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken) =>
        db.Products.AddAsync(product, cancellationToken).AsTask();

    public Task DeleteAsync(Product product, CancellationToken cancellationToken)
    {
        db.Products.Remove(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}