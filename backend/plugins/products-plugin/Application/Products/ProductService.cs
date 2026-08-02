using Microsoft.Extensions.DependencyInjection;
using ProductsPlugin.Application.Contracts;
using ProductsPlugin.Domain.Entities;

namespace ProductsPlugin.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Product> CreateAsync(ProductRequest request, CancellationToken cancellationToken);
    Task<Product?> UpdateAsync(string id, ProductRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public Task<IReadOnlyList<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken) =>
        repository.ListAsync(filter, cancellationToken);

    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<Product> CreateAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = request.ToProduct(Guid.NewGuid().ToString("N"));
        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product?> UpdateAsync(string id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return null;

        request.ApplyTo(product);
        await repository.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return false;

        await repository.DeleteAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductsApplication(this IServiceCollection services) =>
        services.AddScoped<IProductService, ProductService>();
}