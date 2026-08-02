using ProductsPlugin.Domain.Entities;

namespace ProductsPlugin.Application.Products;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task DeleteAsync(Product product, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record ProductFilter(decimal? MinPrice, decimal? MaxPrice, string[]? Availability);